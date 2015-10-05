﻿namespace JosephM.Application.ViewModel.RecordEntry.Metadata
{
    public class FormSection
    {
        public FormSection(string sectionLabel)
        {
            SectionLabel = sectionLabel;
        }

        public string SectionLabel { get; private set; }
    }
}