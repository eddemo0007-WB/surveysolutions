﻿Supervisor.VM.InterviewsBase = function (serviceUrl, interviewDetailsUrl, responsiblesUrl, users, commandExecutionUrl, notifier) {
    Supervisor.VM.InterviewsBase.superclass.constructor.apply(this, [serviceUrl, commandExecutionUrl]);
    
    var self = this;
    
    self.Url = new Url(interviewDetailsUrl);
    self.IsResponsiblesLoading = ko.observable(false);
    self.ResponsiblesUrl = responsiblesUrl;

    self.Responsibles = function (query, sync, pageSize) {
        self.IsResponsiblesLoading(true);
        self.SendRequest(self.ResponsiblesUrl, { query: query, pageSize: pageSize }, function (response) {
            sync(response.Users, response.TotalCountByQuery);
        }, true, true, function() {
            self.IsResponsiblesLoading(false);
        });
    }

    self.SelectedTemplate = ko.observable('');

    self.SelectedResponsible = ko.observable();

    self.SelectedStatus = ko.observable('');
    self.SearchBy = ko.observable('');
    self.AssignmentId = ko.observable('').extend({ rateLimit: 500 });
    self.IsVisiblePrefilledColumns = ko.observable(false);

    self.TemplateName = ko.observable();
    self.UnactiveDateStart = ko.observable();
    self.UnactiveDateEnd = ko.observable();

    self.getFormattedPrefilledQuestions = function(prefilledQuestions) {
        prefilledQuestions.forEach(function(prefilledQuestion) {
            var questionType = prefilledQuestion.Type();
            if (questionType == /*DateTime*/5) {
                prefilledQuestion.Answer(moment(prefilledQuestion.Answer()).format('M/D/YYYY'));
            } else if (questionType == /*Numeric*/4) {
                if (prefilledQuestion.Settings().UseFormating()) {
                    prefilledQuestion.Answer(ko.bindingHandlers.numericformatter.format(prefilledQuestion.Answer()));
                }
            }
        });

        return prefilledQuestions;
    };
    
    self.GetFilterMethod = function () {

        var selectedTemplate = Supervisor.Framework.Objects.isEmpty(self.SelectedTemplate())
            ? { templateId: '', version: '' }
            : JSON.parse(self.SelectedTemplate());

        self.Url.query['templateId'] = selectedTemplate.templateId;
        self.Url.query['templateVersion'] = selectedTemplate.version;
        self.Url.query['status'] = self.SelectedStatus() || "";
        self.Url.query['responsible'] = _.isUndefined(self.SelectedResponsible()) ? "" : self.SelectedResponsible().UserName;
        self.Url.query['searchBy'] = self.SearchBy() || "";
        self.Url.query['assignmentId'] = self.AssignmentId() || "";
        self.Url.query['unactiveDateStart'] = self.UnactiveDateStart() || "";
        self.Url.query['unactiveDateEnd'] = self.UnactiveDateEnd() || "";
        
        if (Modernizr.history) {
            window.history.pushState({}, "Interviews", self.Url.toString());
        }

        return {
            TemplateId: selectedTemplate.templateId,
            TemplateVersion: selectedTemplate.version,
            ResponsibleName: _.isUndefined(self.SelectedResponsible()) ? "" : self.SelectedResponsible().UserName,
            Status: self.SelectedStatus,
            SearchBy: self.SearchBy,
            AssignmentId: self.AssignmentId,
            UnactiveDateStart: self.UnactiveDateStart,
            UnactiveDateEnd: self.UnactiveDateEnd
        };
    };

    self.load = function () {

        self.SelectedTemplate("{\"templateId\": \"" + self.QueryString['templateId'] + "\",\"version\": \"" + self.QueryString['templateVersion'] + "\"}");
        self.SelectedStatus(self.QueryString['status']);

        if (self.QueryString['responsible']) {
            self.SelectedResponsible({ UserName: self.QueryString['responsible'] });
        }
        self.AssignmentId(self.QueryString['assignmentId']);
        self.SearchBy(decodeURIComponent(self.QueryString['searchBy'] || ""));
        self.UnactiveDateStart(self.QueryString['unactiveDateStart']);
        self.UnactiveDateEnd(self.QueryString['unactiveDateEnd']);

        updateTemplateName(self.SelectedTemplate());

        self.Url.query['templateId'] = self.QueryString['templateId'] || "";
        self.Url.query['templateVersion'] = self.QueryString['templateVersion'] || "";
        self.Url.query['status'] = self.QueryString['status'] || "";
        self.Url.query['responsible'] = self.QueryString['responsible'] || "";
        self.Url.query['searchBy'] = self.QueryString['searchBy'] || "";
        self.Url.query['assignmentId'] = self.QueryString['assignmentId'] || "";
        self.Url.query['unactiveDateStart'] = self.QueryString['unactiveDateStart'] || "";
        self.Url.query['unactiveDateEnd'] = self.QueryString['unactiveDateEnd'] || "";

        self.SelectedTemplate.subscribe(
            function (value) {
                updateTemplateName(value);
                self.filter();
            });

        self.SelectedResponsible.subscribe(self.filter);
        self.SelectedStatus.subscribe(self.filter);
        self.AssignmentId.subscribe(self.filter);

        self.search();

        self.InitPrefilledColumnsAndShowSearchBarIfNeed();

        $('.selectpicker').selectpicker('refresh');
    };

    self.sendCommandAfterFilterAndConfirm = function (commandName,
        parametersFunc,
        filterFunc,
        messageTemplateId,
        continueMessageTemplateId,
        onSuccessCommandExecuting,
        onCancelConfirmation)
    {
            var filteredItems = self.GetSelectedItemsAfterFilter(filterFunc);

            var messageHtml = self.getBindedHtmlTemplate(messageTemplateId, filteredItems);

            if (filteredItems.length === 0) {
                notifier.alert('', messageHtml);
                return;
            }

            messageHtml += $(continueMessageTemplateId).html();

            notifier.confirm('', messageHtml, function (result) {
                if (result) {
                    self.sendCommand(commandName, parametersFunc, filteredItems, onSuccessCommandExecuting);
                } else {
                    if (!_.isUndefined(onCancelConfirmation)) {
                        onCancelConfirmation();
                    }
                }
            });
    };

    self.sendCommand = function (commandName, parametersFunc, items, onSuccessCommandExecuting) {
        var commands = ko.utils.arrayMap(items, function (rawItem) {
            var item = ko.mapping.toJS(rawItem);
            return ko.toJSON(parametersFunc(item));
        });

        var command = {
            type: commandName,
            commands: commands
        };

        self.SendCommands(command, function () {
            if (!_.isUndefined(onSuccessCommandExecuting))
                onSuccessCommandExecuting();
            self.search();
        }, true);
    };

    var updateTemplateName = function(value) {
        self.TemplateName($("#templateSelector option[value='" + value + "']").text());
    };

    self.ToggleVisiblePrefilledColumns = function () {
        self.IsVisiblePrefilledColumns(!self.IsVisiblePrefilledColumns());
        return false;
    };

    self.InitPrefilledColumnsAndShowSearchBarIfNeed = function() {
        $('.dataTables_filter label').on('click', function (e) {
            if (e.target !== this)
                return;

            if ($(this).hasClass("active")) {
                $(this).removeClass("active");
            } else {
                $(this).addClass("active");
                $(this).children("input[type='search']").delay(200).queue(function () { $(this).focus(); $(this).dequeue(); });
            }
        });

        if (self.Url.query['searchBy'].length > 0) {
            $('.dataTables_filter label').addClass("active");
        }

        var contextMenuOptions = {
            autoHide: true,
            build: function ($trigger, e) {
                if ($trigger.hasClass("no-menu")) return false;
                var selectedRow = ko.dataFor($trigger[0]);
                self.unselectAll();
                selectedRow.IsSelected(true);
                var items = self.BuildMenuItem(selectedRow);
                return { items: items };
            },
            trigger: 'left'
        };


        $.contextMenu(_.assign({ selector: "#interviews tr td:not(:first-child)" }, contextMenuOptions));
        $.contextMenu(_.assign({ selector: ".row-unit" }, contextMenuOptions));
    };

    self.ShowStatusHistory = function (url, interview) {
        var statusHistoryTemplateId = "#interview-status-history-template";
        var modalId = '#statusHistoryModal';
        
        self.SendRequest(url,
            { interviewId: interview.InterviewId() },
            function(statusHistory) {

                $(modalId).parent().remove();

                var historyModalModel = {
                    key: interview.Key(),
                    interviewUrl: $detailsUrl + '/' + interview.InterviewId(),
                    responsible: interview.ResponsibleName(),
                    isResponsibleInterviewer: interview.ResponsibleRole() == 4,
                    statusHistory: statusHistory
                };

                $('body').append($("<div/>").html($(statusHistoryTemplateId).html())[0]);
               
                ko.applyBindings(historyModalModel, $(modalId)[0]);

                $(modalId).modal('show', { backdrop: true });
            },
            false,
            false);
    }
};
Supervisor.Framework.Classes.inherit(Supervisor.VM.InterviewsBase, Supervisor.VM.ListView);
