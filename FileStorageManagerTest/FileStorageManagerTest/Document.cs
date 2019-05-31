using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Model
{
    public enum DocumentStatus
    {
        Unprocessed,
        Processed,
        //CompleteUnprocessed
    }

    public enum LateDocumentStatus
    {
        [Description("None")]
        None,
        [Description("In Queue")]
        InQueue,
        [Description("Processed")]
        Processed
    }
    public enum DocType
    {
        [Description("Scheduled Report")]
        ScheduledReport = 1
    }

    public class Document 
    {
        public Document()
        {
            IsValid = true;
        }

        public virtual int ID { get; set; }
        public virtual String Description { get; set; }
        public virtual String FileName { get; set; }
        public virtual DocType? DocType { get; set; }
        private string _originalFileName;
        public virtual string OriginalFileName
        {
            get { return _originalFileName; }

            set
            {
                if (!DocType.HasValue)
                {
                    DocType = GetDocTypeFromName(value);
                }

                _originalFileName = value;
            }
        }
        public virtual DateTime Date { get; set; }
        public virtual string DateString { get; set; }
        public virtual DocumentStatus Status { get; set; }
        public virtual bool IsValid { get; set; }
        public virtual bool IsExpired { get; set; }
        public virtual DateTime FirstDayOfWeek { get; protected set; }
        public virtual DateTime CreateDate { get; set; }
        public virtual string CreatedBy { get; set; }
        public virtual DateTime? LastModified { get; set; }
        public virtual string ModifiedBy { get; set; }

        public virtual bool? DeleteRawFile { get; set; }
        public virtual DateTime? DeleteRawFileDate { get; set; }

        public virtual int PageCount { get; set; }
        public virtual string ContentType { get; set; }

        private DocType? GetDocTypeFromName(string name)
        {
            var result = new DocType?();

            if (string.IsNullOrWhiteSpace(name))
            {
                return result;
            }

            name = name.ToLower().Replace(" ", string.Empty).Replace("_", string.Empty);

            var matchCount = 0;

            var docTypeString = "scheduledreport";

            if (name.Contains(docTypeString))
            {
                result = Model.DocType.ScheduledReport;

                matchCount++;
            }

            return matchCount == 1 ? result : new DocType?();
        }

        public virtual bool IsAzure { get; set; }
    }
}
