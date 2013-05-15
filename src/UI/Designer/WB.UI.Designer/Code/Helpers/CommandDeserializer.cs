namespace WB.UI.Designer.Code.Helpers
{
    using System;
    using System.Collections.Generic;

    using Main.Core.Commands.Questionnaire.Group;
    using Main.Core.Commands.Questionnaire.Question;

    using Ncqrs.Commanding;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    internal class CommandDeserializer : ICommandDeserializer
    {
        private static readonly Dictionary<string, Type> knownCommandTypes = new Dictionary<string, Type>
        {
            { "UpdateGroup", typeof(UpdateGroupCommand) },
            { "AddGroup", typeof(AddGroupCommand) },
            { "DeleteGroup", typeof(DeleteGroupCommand) },
            { "MoveGroup", typeof(MoveGroupCommand) },
            { "UpdateQuestion", typeof(UpdateQuestionCommand) },
            { "AddQuestion", typeof(AddQuestionCommand) },
            { "DeleteQuestion", typeof(DeleteQuestionCommand) },
            { "MoveQuestion", typeof(MoveQuestionCommand) },
        };

        public ICommand Deserialize(string commandType, string serializedCommand)
        {
            Type resultCommandType = GetTypeOfResultCommandOrThrowArgumentException(commandType);

            return (ICommand) JsonConvert.DeserializeObject(serializedCommand, resultCommandType);
        }

        private static Type GetTypeOfResultCommandOrThrowArgumentException(string commandType)
        {
            if (!knownCommandTypes.ContainsKey(commandType))
                throw new CommandDeserializationException(string.Format("Command type '{0}' is not supported.", commandType));

            return knownCommandTypes[commandType];
        }
    }
}