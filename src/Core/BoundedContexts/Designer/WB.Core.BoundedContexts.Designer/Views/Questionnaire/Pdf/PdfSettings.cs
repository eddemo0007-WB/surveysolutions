namespace WB.Core.BoundedContexts.Designer.Views.Questionnaire.Pdf
{
    public class PdfSettings
    {
        public PdfSettings(int instructionsExcerptLength, int expressionExcerptLength, int optionsExcerptCount, int minAmountOfDigitsInCodes, int attachmentSize)
        {
            this.InstructionsExcerptLength = instructionsExcerptLength;
            this.ExpressionExcerptLength = expressionExcerptLength;
            this.OptionsExcerptCount = optionsExcerptCount;
            this.MinAmountOfDigitsInCodes = minAmountOfDigitsInCodes;
            this.AttachmentSize = attachmentSize;
        }

        public int InstructionsExcerptLength { get; }
        public int ExpressionExcerptLength { get; }
        public int OptionsExcerptCount { get; }
        public int MinAmountOfDigitsInCodes { get; }
        public int AttachmentSize { get; set; }
    }
}