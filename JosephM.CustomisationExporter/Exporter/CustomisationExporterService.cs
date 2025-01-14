﻿using JosephM.Core.Extentions;
using JosephM.Core.Log;
using JosephM.Core.Service;
using JosephM.Core.Utility;
using JosephM.Record.Extentions;
using JosephM.Record.Metadata;
using JosephM.Record.Xrm.XrmRecord;
using JosephM.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JosephM.CustomisationExporter.Exporter
{
    public class CustomisationExporterService :
        ServiceBase<CustomisationExporterRequest, CustomisationExporterResponse, CustomisationExporterResponseItem>
    {
        public CustomisationExporterService(XrmRecordService service)
        {
            Service = service;
        }

        private XrmRecordService Service { get; set; }

        public override void ExecuteExtention(CustomisationExporterRequest request,
            CustomisationExporterResponse response,
            ServiceRequestController controller)
        {
            response.Folder = request.SaveToFolder.FolderPath;
            controller.LogLiteral("Loading Metadata");
            
            ProcessForEntities(request, response, controller.Controller);

            if ((request.Fields || request.FieldOptionSets) && request.IncludeAllRecordTypes)
            {
                controller.UpdateProgress(0, 1, "Loading All Fields.....");
                Service.LoadFieldsForAllEntities();
            }
            if ((request.Fields || request.Relationships) && request.IncludeAllRecordTypes)
            {
                controller.UpdateProgress(0, 1, "Loading All Relationships.....");
                Service.LoadRelationshipsForAllEntities();
            }
            ProcessForFields(request, response, controller.Controller);

            ProcessForRelationships(request, response, controller.Controller);
            ProcessForOptionSets(request, response, controller.Controller);

            response.Folder = request.SaveToFolder.FolderPath;

            if (request.Format == CustomisationExporterRequest.FileFormat.Xlsx)
            {
                var excelFileName = "Customisation Export " + DateTime.Now.ToFileTime() + ".xlsx";
                ExcelUtility.CreateXlsx(request.SaveToFolder.FolderPath, excelFileName, response.GetListsToOutput());
                response.ExcelFileName = excelFileName;
            }
            else
            {
                foreach(var item in response.GetListsToOutput())
                {
                    var csvName = item.Key + " " + DateTime.Now.ToFileTime() + ".csv";
                    CsvUtility.CreateCsv(request.SaveToFolder.FolderPath, csvName, item.Value);
                }
            }

            response.Message = "The Export is Complete";
        }

        private void ProcessForRelationships(CustomisationExporterRequest request,
            CustomisationExporterResponse response,
            LogController controller)
        {
            if (request.Relationships)
            {
                var allRelationship = new List<RelationshipExport>();
                var types = GetRecordTypesToExport(request).OrderBy(t => Service.GetDisplayName(t)).ToArray();
                var count = types.Count();
                var manyToManyDone = new List<string>();
                for (var i = 0; i < count; i++)
                {
                    var thisType = types.ElementAt(i);
                    var thisTypeLabel = Service.GetDisplayName(thisType);
                    controller.UpdateProgress(i, count, "Exporting Relationships For " + thisTypeLabel);
                    try
                    {
                        var relationships = Service.GetManyToManyRelationships(thisType);
                        for (var j = 0; j < relationships.Count(); j++)
                        {
                            var relationship = relationships.ElementAt(j);
                            try
                            {
                                if (relationship.RecordType1 == thisType
                                    ||
                                    (!request.DuplicateManyToManyRelationshipSides &&
                                     !manyToManyDone.Contains(relationship.SchemaName))
                                    )
                                {
                                    allRelationship.Add(new RelationshipExport(relationship.SchemaName,
                                        relationship.RecordType1, relationship.RecordType2,
                                        relationship.IsCustomRelationship, relationship.RecordType1DisplayRelated,
                                        relationship.RecordType2DisplayRelated
                                        , relationship.Entity1IntersectAttribute, relationship.Entity2IntersectAttribute,
                                        RelationshipExport.RelationshipType.ManyToMany,
                                        relationship.RecordType1UseCustomLabel, relationship.RecordType2UseCustomLabel,
                                        relationship.RecordType1CustomLabel, relationship.RecordType2CustomLabel
                                        , relationship.RecordType1DisplayOrder, relationship.RecordType2DisplayOrder, relationship.MetadataId
                                        , null));
                                    manyToManyDone.Add(relationship.SchemaName);
                                }
                                if (relationship.RecordType2 == thisType
                                    && (request.DuplicateManyToManyRelationshipSides
                                        || (!manyToManyDone.Contains(relationship.SchemaName))))
                                {
                                    allRelationship.Add(new RelationshipExport(relationship.SchemaName,
                                        relationship.RecordType2, relationship.RecordType1,
                                        relationship.IsCustomRelationship, relationship.RecordType2DisplayRelated,
                                        relationship.RecordType1DisplayRelated
                                        , relationship.Entity2IntersectAttribute, relationship.Entity1IntersectAttribute,
                                        RelationshipExport.RelationshipType.ManyToMany,
                                        relationship.RecordType2UseCustomLabel, relationship.RecordType1UseCustomLabel,
                                        relationship.RecordType2CustomLabel, relationship.RecordType1CustomLabel
                                        , relationship.RecordType2DisplayOrder, relationship.RecordType1DisplayOrder
                                        , relationship.MetadataId, null));
                                    manyToManyDone.Add(relationship.SchemaName);
                                }
                            }
                            catch (Exception ex)
                            {
                                response.AddResponseItem(
                                    new CustomisationExporterResponseItem("Error Exporting Relationship",
                                        relationship.SchemaName, ex));
                            }
                        }
                        if (request.IncludeOneToManyRelationships)
                        {
                            var oneTorelationships = Service.GetOneToManyRelationships(thisType);
                            for (var j = 0; j < oneTorelationships.Count(); j++)
                            {
                                var relationship = oneTorelationships.ElementAt(j);
                                try
                                {
                                    var isCustomRelationship = Service.FieldExists(relationship.ReferencingAttribute, relationship.ReferencingEntity)
                                        && Service.GetFieldMetadata(relationship.ReferencingAttribute, relationship.ReferencingEntity).IsCustomField;
                                    allRelationship.Add(new RelationshipExport(relationship.SchemaName,
                                        relationship.ReferencedEntity, relationship.ReferencingEntity,
                                        isCustomRelationship, false,
                                        relationship.DisplayRelated
                                        , null, relationship.ReferencingAttribute,
                                        RelationshipExport.RelationshipType.OneToMany, false, relationship.IsCustomLabel,
                                        null, relationship.GetRelationshipLabel
                                        , 0, relationship.DisplayOrder, relationship.MetadataId, relationship.DeleteCascadeConfiguration));
                                }
                                catch (Exception ex)
                                {
                                    response.AddResponseItem(
                                        new CustomisationExporterResponseItem("Error Exporting Relationship",
                                            relationship.SchemaName, ex));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        response.AddResponseItem(new CustomisationExporterResponseItem("Error Exporting Relationships",
                            thisType, ex));
                    }
                }
                response.AddListToOutput("Relationships", allRelationship);
            }
        }

        private static IEnumerable<RecordFieldType> OptionSetTypes
        {
            get { return new[] {RecordFieldType.Picklist, RecordFieldType.Status}; }
        }

        private void ProcessForOptionSets(CustomisationExporterRequest request, CustomisationExporterResponse response,
            LogController controller)
        {
            if (!request.FieldOptionSets && !request.SharedOptionSets)
                return;

            var allOptions = new List<OptionExport>();

            if (request.FieldOptionSets)
            {
                var types = GetRecordTypesToExport(request).OrderBy(t => Service.GetDisplayName(t)).ToArray();
                var count = types.Count();
                for (var i = 0; i < count; i++)
                {
                    var thisType = types.ElementAt(i);
                    var thisTypeLabel = Service.GetDisplayName(thisType);
                    controller.UpdateProgress(i, count, "Exporting Options For " + thisTypeLabel);
                    try
                    {
                        var fields =
                            Service.GetFields(thisType)
                                .Where(f => !Service.GetFieldLabel(f, thisType).IsNullOrWhiteSpace())
                                .Where(f => OptionSetTypes.Contains(Service.GetFieldType(f, thisType)))
                                .ToArray();
                        var numberOfFields = fields.Count();
                        for (var j = 0; j < numberOfFields; j++)
                        {
                            var field = fields.ElementAt(j);
                            var fieldLabel = Service.GetFieldLabel(field, thisType);
                            try
                            {
                                var keyValues = Service.GetPicklistKeyValues(field, thisType);
                                foreach (var keyValue in keyValues)
                                {
                                    allOptions.Add(new OptionExport(thisTypeLabel, thisType,
                                        fieldLabel, field, keyValue.Key, keyValue.Value, false, null,
                                        JoinTypeAndFieldName(field, thisType)));
                                }
                            }
                            catch (Exception ex)
                            {
                                response.AddResponseItem(
                                    new CustomisationExporterResponseItem("Error Exporting Options For Field",
                                        field, ex));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        response.AddResponseItem(
                            new CustomisationExporterResponseItem("Error Exporting Record Type Options",
                                thisType, ex));
                    }
                }
            }
            if (request.SharedOptionSets)
            {
                var sets = Service.GetSharedPicklists();
                var countSets = sets.Count();
                for (var i = 0; i < countSets; i++)
                {
                    var thisSet = sets.ElementAt(i);
                    controller.UpdateProgress(i, countSets, "Exporting Share Option Sets");
                    try
                    {
                        var options = thisSet.PicklistOptions;
                        var label = thisSet.DisplayName;
                        foreach (var option in options)
                        {
                            allOptions.Add(new OptionExport(null, null,
                                null, null, option.Key, option.Value, true, thisSet.SchemaName, label));
                        }
                    }
                    catch (Exception ex)
                    {
                        response.AddResponseItem(
                            new CustomisationExporterResponseItem("Error Exporting Shared Option Set",
                                thisSet.SchemaName, ex));
                    }
                }
            }
            response.AddListToOutput("Option Sets", allOptions);
        }

        private void ProcessForFields(CustomisationExporterRequest request, CustomisationExporterResponse response,
            LogController controller)
        {
            if (request.Fields)
            {
                var allFields = new List<FieldExport>();
                var types = GetRecordTypesToExport(request).OrderBy(t => Service.GetDisplayName(t)).ToArray();
                var count = types.Count();
                for (var i = 0; i < count; i++)
                {
                    var thisType = types.ElementAt(i);
                    var thisTypeLabel = Service.GetDisplayName(thisType);
                    var primaryField = Service.GetPrimaryField(thisType);
                    controller.UpdateProgress(i, count, "Exporting Fields For " + thisTypeLabel);
                    try
                    {
                        var fields =
                            Service.GetFields(thisType)
                                .Where(f => !Service.GetFieldLabel(f, thisType).IsNullOrWhiteSpace())
                                .ToArray();
                        var numberOfFields = fields.Count();
                        for (var j = 0; j < numberOfFields; j++)
                        {
                            var field = fields.ElementAt(j);
                            var fieldLabel = Service.GetFieldLabel(field, thisType);
                            try
                            {
                                var thisFieldType = Service.GetFieldType(field, thisType);
                                var displayRelated = Service.IsLookup(field, thisType) &&
                                                     Service.GetFieldMetadata(field, thisType).IsDisplayRelated;
                                var picklist = thisFieldType == RecordFieldType.Picklist
                                    ? CreatePicklistName(field, thisType)
                                    : "N/A";
                                string referencedType = "N/A";
                                if (Service.IsLookup(field, thisType))
                                {
                                    referencedType = Service.GetLookupTargetType(field, thisType);
                                }
                                var isString = Service.IsString(field, thisType);
                                int maxLength = isString ? Service.GetMaxLength(field, thisType) : -1;
                                var textFormat = thisFieldType == RecordFieldType.String
                                    ? Service.GetFieldMetadata(field, thisType).TextFormat.ToString()
                                    : null;
                                var includeTime = false;
                                var dateBehaviour = "N/A";
                                var integerFormat = thisFieldType == RecordFieldType.Integer
                                    ? Service.GetFieldMetadata(field, thisType).IntegerFormat.ToString()
                                    : null;
                                var minValue = "-1";
                                var maxValue = "-1";
                                var precision = "-1";
                                if (thisFieldType == RecordFieldType.Date)
                                {
                                    includeTime = Service.GetFieldMetadata(field, thisType).IncludeTime;
                                    dateBehaviour = Service.GetFieldMetadata(field, thisType).DateBehaviour;
                                }
                                if (thisFieldType == RecordFieldType.Decimal)
                                {
                                    minValue =
                                        Service.GetFieldMetadata(field, thisType)
                                            .MinValue.ToString(CultureInfo.InvariantCulture);
                                    maxValue =
                                        Service.GetFieldMetadata(field, thisType)
                                            .MaxValue.ToString(CultureInfo.InvariantCulture);
                                    precision =
                                        Service.GetFieldMetadata(field, thisType)
                                            .DecimalPrecision.ToString(CultureInfo.InvariantCulture);
                                }
                                if (thisFieldType == RecordFieldType.Double)
                                {
                                    minValue =
                                        Service.GetFieldMetadata(field, thisType)
                                            .MinValue.ToString(CultureInfo.InvariantCulture);
                                    maxValue =
                                        Service.GetFieldMetadata(field, thisType)
                                            .MaxValue.ToString(CultureInfo.InvariantCulture);
                                    precision =
                                        Service.GetFieldMetadata(field, thisType)
                                            .DecimalPrecision.ToString(CultureInfo.InvariantCulture);
                                }
                                if (thisFieldType == RecordFieldType.Integer)
                                {
                                    minValue =
                                        Service.GetFieldMetadata(field, thisType)
                                            .MinValue.ToString(CultureInfo.InvariantCulture);
                                    maxValue =
                                        Service.GetFieldMetadata(field, thisType)
                                            .MaxValue.ToString(CultureInfo.InvariantCulture);
                                }
                                if (thisFieldType == RecordFieldType.Money)
                                {
                                    minValue =
                                        Service.GetFieldMetadata(field, thisType)
                                            .MinValue.ToString(CultureInfo.InvariantCulture);
                                    maxValue =
                                        Service.GetFieldMetadata(field, thisType)
                                            .MaxValue.ToString(CultureInfo.InvariantCulture);
                                }

                                var fieldExport = new FieldExport(thisTypeLabel, thisType,
                                    fieldLabel, field, Service.GetFieldType(field, thisType),
                                    Service.GetFieldMetadata(field, thisType).IsCustomField,
                                    Service.GetFieldMetadata(field, thisType).IsMandatory,
                                    Service.GetFieldMetadata(field, thisType).Description, primaryField == field,
                                    Service.GetFieldMetadata(field, thisType).Audit,
                                    Service.GetFieldMetadata(field, thisType).Searchable
                                    , displayRelated, referencedType, maxLength, textFormat, integerFormat, dateBehaviour, includeTime, minValue,
                                    maxValue, precision, picklist, Service.GetFieldMetadata(field, thisType).MetadataId, Service.GetFieldMetadata(field, thisType).IsMultiSelect);
                                if (Service.IsString(field, thisType))
                                    fieldExport.MaxLength = Service.GetMaxLength(field, thisType);
                                allFields.Add(fieldExport);
                            }
                            catch (Exception ex)
                            {
                                response.AddResponseItem(new CustomisationExporterResponseItem("Error Exporting Field",
                                    field, ex));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        response.AddResponseItem(
                            new CustomisationExporterResponseItem("Error Exporting Record Type Fields",
                                thisType, ex));
                    }
                }
                response.AddListToOutput("Fields", allFields);
            }
        }

        private string CreatePicklistName(string field, string thisType)
        {
            return Service.GetFieldMetadata(field, thisType).IsSharedPicklist
                ? Service.GetFieldMetadata(field, thisType).PicklistName
                : JoinTypeAndFieldName(field, thisType);
        }

        private static string JoinTypeAndFieldName(string field, string thisType)
        {
            return string.Format("{0}.{1}", thisType, field);
        }

        private void ProcessForEntities(CustomisationExporterRequest request, CustomisationExporterResponse response,
            LogController controller)
        {
            if (request.Entities)
            {
                var allEntities = new List<EntityExport>();
                var types = GetRecordTypesToExport(request).OrderBy(t => Service.GetDisplayName(t)).ToArray();
                var count = types.Count();
                for (var i = 0; i < count; i++)
                {
                    var thisType = types.ElementAt(i);
                    var thisTypeLabel = Service.GetDisplayName(thisType);
                    controller.UpdateProgress(i, count, "Exporting Record Type " + thisTypeLabel);
                    try
                    {
                        var mt = Service.GetRecordTypeMetadata(thisType);
                        allEntities.Add(new EntityExport(
                            mt.DisplayName,
                            thisType,
                            mt.IsCustomType,
                            mt.RecordTypeCode,
                            mt.CollectionName,
                            mt.Description,
                            mt.Audit,
                            mt.IsActivityType,
                            mt.Notes,
                            mt.Activities,
                            mt.Connections,
                            mt.MailMerge,
                            mt.Queues,
                            mt.MetadataId,
                            mt.ChangeTracking,
                            mt.EntitySetName,
                            mt.DocumentManagement,
                            mt.QuickCreate
                            ));
                    }
                    catch (Exception ex)
                    {
                        response.AddResponseItem(new CustomisationExporterResponseItem("Error Exporting Record Type",
                            thisType, ex));
                    }
                }

                response.AddListToOutput("Record Types", allEntities);
            }
        }

        private IEnumerable<string> GetRecordTypesToExport(CustomisationExporterRequest request)
        {
            var recordTypes = request.IncludeAllRecordTypes
                ? Service.GetAllRecordTypes()
                : request.RecordTypes.Select(r => r.RecordType.Key);
            return recordTypes.Where(r => !Service.GetDisplayName(r).IsNullOrWhiteSpace()).ToArray();
        }
    }
}