﻿angular.module('designerApp')
    .factory('errorReportingInterceptor', [
        '$q', 'notificationService',
        function ($q, notificationService) {
            return {
                responseError: function (rejection) {
                    if (rejection.status === 406 || rejection.status === 403 || rejection.status === 400) {
                        if (rejection.data.message) {
                            notificationService.notice(rejection.data.message);
                        } else {
                            notificationService.notice(rejection.data.Message);
                        }
                    }
                    else if (rejection.status === 404) {
                        notificationService.notice('Entry was not found in questionnaire, please select another.');
                    } else {
                        notificationService.error('Request failed unexpectedly.');
                    }
                    return $q.reject(rejection);
                }
            };
        }]);
