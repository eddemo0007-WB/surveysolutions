using System;
using WB.Core.BoundedContexts.Designer.ValueObjects;

namespace WB.Core.BoundedContexts.Designer.CodeGenerationV2.Models
{
    public class StaticTextModel
    {
        public Guid Id { set; get; }
        public string Variable { set; get; }
        public string ClassName { get; set; }
        public RosterScope RosterScope { get; set; }
    }
}