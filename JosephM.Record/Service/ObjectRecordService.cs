﻿#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JosephM.Core.Attributes;
using JosephM.Core.Extentions;
using JosephM.Core.FieldType;
using JosephM.Record.IService;
using JosephM.Record.Metadata;
using JosephM.Record.Extentions;
using JosephM.Core.AppConfig;
using JosephM.Core.Service;
using JosephM.Record.Attributes;
using JosephM.Record.Query;

#endregion

namespace JosephM.Record.Service
{
    /// <summary>
    ///     Implementation Of IRecordService To Interface To A A Standard CLR Object
    /// </summary>
    public class ObjectRecordService : RecordServiceBase
    {
        public ObjectRecordService(object objectToEnter, IResolveObject objectResolver)
            : this(objectToEnter, null, null, objectResolver)
        {
        }

        public ObjectRecordService(object objectToEnter, IRecordService lookupService,
            IDictionary<string, IEnumerable<string>> optionSetLimitedValues, IResolveObject objectResolver)
            : this(objectToEnter, lookupService, optionSetLimitedValues, null, null, objectResolver)
        {
        }

        public ObjectRecordService(object objectToEnter, IRecordService lookupService,
            IDictionary<string, IEnumerable<string>> optionSetLimitedValues, ObjectRecordService parentService, string parentServiceReference, IResolveObject objectResolver)
        {
            ObjectResolver = objectResolver;
            ParentServiceReference = parentServiceReference;
            ParentService = parentService;
            ObjectToEnter = objectToEnter;
            _lookupService = lookupService;
            OptionSetLimitedValues = optionSetLimitedValues;
            var objectTypeFieldMetadata = new List<FieldMetadata>();

            var type = ObjectToEnter.GetType();
            objectTypeFieldMetadata.AddRange(RecordMetadataFactory.GetClassFieldMetadata(type));
            FieldMetadata.Add(type.Name, objectTypeFieldMetadata);
            foreach (var field in objectTypeFieldMetadata.Where(f => f.FieldType == RecordFieldType.Enumerable))
            {
                //need to add the field metadata for any nested types
                var propertyType = type.GetProperty(field.SchemaName).PropertyType;
                var genericEnumerableType = propertyType.GetGenericArguments()[0];
                if (!FieldMetadata.ContainsKey(genericEnumerableType.Name))
                {
                    var metadata = RecordMetadataFactory.GetClassFieldMetadata(genericEnumerableType);
                    FieldMetadata.Add(genericEnumerableType.Name, metadata);
                }
            }
        }

        private Type ObjectType
        {
            get { return ObjectToEnter.GetType(); }
        }

        private readonly IRecordService _lookupService;

        public override IRecordService LookupService
        {
            get
            {
                return _lookupService;
            }
        }

        public IDictionary<string, IEnumerable<string>> OptionSetLimitedValues { get; private set; }

        public object ObjectToEnter { get; set; }

        private IResolveObject ObjectResolver { get; set; }

        private readonly Dictionary<string, IEnumerable<FieldMetadata>> _fieldMetadata =
            new Dictionary<string, IEnumerable<FieldMetadata>>();

        //DON'T HAVE THIS REFERENCE ITSELF OR WILL HAVE INFINITE LOOP!!
        private ObjectRecordService ParentService { get; set; }
        private string ParentServiceReference { get; set; }

        private Dictionary<string, IEnumerable<FieldMetadata>> FieldMetadata
        {
            get { return _fieldMetadata; }
        }

        public override IRecord NewRecord(string recordType)
        {
            //need to get the class constructor and instantiate
            var type = GetClassType(recordType);
            if (!type.HasParameterlessConstructor())
                throw new NullReferenceException(
                    string.Format("Type {0} Does Not Have A Parameterless Constructor To Create The Object", recordType));
            return new ObjectRecord(type.CreateFromParameterlessConstructor());
        }

        public override IRecord Get(string recordType, string id)
        {
            throw new NotImplementedException();
        }

        public Type GetClassType(string recordType)
        {
            Type type = null;
            if (recordType == ObjectType.Name)
                type = ObjectType;
            else
            {
                var fieldMetadata = GetFieldMetadata(ObjectType.Name);
                foreach (var metadata in fieldMetadata.Where(m => m.FieldType == RecordFieldType.Enumerable))
                {
                    if (((EnumerableFieldMetadata)metadata).EnumeratedType == recordType)
                    {
                        var propertyName = metadata.SchemaName;
                        type = ObjectType.GetProperty(propertyName).PropertyType.GetGenericArguments()[0];
                        break;
                    }
                }
            }
            if (type == null)
                throw new NullReferenceException(string.Format("Could Not Resolve Class Of Type {0}", recordType));
            return type;
        }



        public override string Create(IRecord record, IEnumerable<string> fields)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string recordType, string id)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IRecord> GetFirstX(string type, int x, IEnumerable<string> fields, IEnumerable<Condition> conditions, IEnumerable<SortExpression> sort)
        {
            throw new NotImplementedException();
        }

        public override IsValidResponse VerifyConnection()
        {
            var response = new IsValidResponse();
            if(ObjectToEnter == null)
                response.AddInvalidReason("The object to enter is null");
            return response;
        }

        public override IEnumerable<IRecord> GetLinkedRecords(string linkedEntityType, string entityTypeFrom,
            string linkedEntityLookup, string entityFromId)
        {
            var propertyValue = ObjectToEnter.GetPropertyValue(linkedEntityLookup);
            if (propertyValue == null)
                return new IRecord[0];
            var enumerable = ((IEnumerable)propertyValue);
            var objectList = new List<ObjectRecord>();
            foreach (var item in enumerable)
            {
                objectList.Add(new ObjectRecord(item));
            }
            return objectList;
        }

        public override IEnumerable<IFieldMetadata> GetFieldMetadata(string recordType)
        {
            if (FieldMetadata.ContainsKey(recordType))
                return FieldMetadata[recordType];
            throw new ArgumentOutOfRangeException("recordType",
                "No Field Metadata Has Been Created For Type " + recordType);
        }

        public override void Update(IRecord record, IEnumerable<string> changedPersistentFields)
        {
            throw new NotImplementedException();
        }

        public override IRecordTypeMetadata GetRecordTypeMetadata(string recordType)
        {
            var type = GetClassType(recordType);
            return new ObjectRecordMetadata()
            {
                SchemaName = recordType,
                DisplayName = type.GetDisplayName(),
                CollectionName = type.GetDisplayName() + "s"
            };
        }

        public bool HasSetAccess(string fieldName, string recordType)
        {
            return GetPropertyInfo(fieldName, recordType).CanWrite;
        }

        public PropertyInfo GetPropertyInfo(string fieldName, string recordType)
        {
            return GetClassType(recordType).GetProperty(fieldName);
        }

        public IEnumerable<PropertyInfo> GetPropertyInfos(string recordType)
        {
            return GetClassType(recordType).GetProperties();
        }

        /// <summary>
        /// If Nullable Return The Nullable Type
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="recordType"></param>
        /// <returns></returns>
        public Type GetPropertyType(string fieldName, string recordType)
        {
            var type = GetClassType(recordType).GetProperty(fieldName).PropertyType;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GenericTypeArguments[0];
            return type;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="recordType"></param>
        /// <param name="dependantValue">Various uses...</param>
        /// <returns></returns>
        public override IEnumerable<PicklistOption> GetPicklistKeyValues(string fieldName, string recordType, string dependantValue, IRecord record)
        {
            //if the property is type RecordType
            //then get the record types from the lookup service
            var fieldType = this.GetFieldType(fieldName, recordType);
            switch (fieldType)
            {
                case RecordFieldType.RecordType:
                    {
                        var lookupService = GetLookupService(fieldName, recordType, dependantValue, record);

                        if (OptionSetLimitedValues != null
                            && OptionSetLimitedValues.ContainsKey(fieldName)
                            && OptionSetLimitedValues[fieldName].Any())
                            return OptionSetLimitedValues[fieldName]
                                .Select(at => new RecordType(at, LookupService.GetRecordTypeMetadata(at).DisplayName))
                                .OrderBy(rt => rt.Value)
                                .ToArray();
                        else
                        {
                            return lookupService == null
                                ? new RecordType[0]
                                : lookupService.GetAllRecordTypes()
                                .Select(r => new RecordType(r, lookupService.GetRecordTypeMetadata(r).DisplayName))
                                .ToArray();
                        }
                    }
                case RecordFieldType.RecordField:
                    {
                        if (dependantValue == null)
                            return new RecordField[0];
                        var type = dependantValue;
                        string parentReference = null;
                        if (dependantValue != null && dependantValue.Contains(':'))
                        {
                            type = ((string)dependantValue).Split(':').ElementAt(0);
                            parentReference = ((string)dependantValue).Split(':').ElementAt(1);
                        }
                        var lookupService = GetLookupService(fieldName, recordType, parentReference, record);

                        return type.IsNullOrWhiteSpace()
                        ? new RecordField[0]
                        : lookupService
                            .GetFields(type)
                            .Select(f => new RecordField(f, lookupService.GetFieldMetadata(f, type).DisplayName))
                            .Where(f => !f.Value.IsNullOrWhiteSpace())
                            .OrderBy(f => f.Value)
                            .ToArray();
                    }
                case RecordFieldType.Picklist:
                {
                    var type = GetPropertyType(fieldName, recordType);
                    var options = PicklistOption.GenerateEnumOptions(type);
                    var propertyInfo = GetPropertyInfo(fieldName, recordType);
                    var limitAttribute = propertyInfo.GetCustomAttribute<LimitPicklist>();
                    if (limitAttribute != null)
                        options =
                            options.Where(
                                o =>
                                    limitAttribute.ToInclude.Select(kv => Convert.ToInt32(kv).ToString())
                                        .Contains(o.Key))
                                        .ToArray();
                    return options;
                }
            }
            throw new ArgumentOutOfRangeException(
                string.Format("GetPicklistOptions Not Implemented For Fiel Of Type {0} Field: {1} Type {2}", fieldType,
                    fieldName, recordType));
        }

        private object _lockObject = new object();
        private Dictionary<object, IRecordService> _serviceConnections = new Dictionary<object, IRecordService>();

        public override IRecordService GetLookupService(string fieldName, string recordType, string reference, IRecord record)
        {
            //needed to implement several inspections to get service connections for other properties with ConnectionFor attributes

            //may be object in a grid where have to check other properties for object in that row
            //or property of object in main form
            //or property of object in a parent form (accessed through the parent service)
            if (record != null && (!(record is ObjectRecord)))
                throw new TypeLoadException(string.Format("Expected {0} Of Type {1}", typeof(IRecord).Name, typeof(ObjectRecord).Name));
            IRecordService lookupService = null;
            if (record != null)
            {
                lookupService = GetLookupServiceForConnectionFor(fieldName, ((ObjectRecord)record).Instance);
                if (lookupService != null)
                    return lookupService;
            }
            //try all valid combinations of reference and field name
            lookupService = GetLookupServiceForConnectionFor(reference, ObjectToEnter);
            if (lookupService != null)
                return lookupService;
            lookupService = GetLookupServiceForConnectionFor(reference + "." + fieldName, ObjectToEnter);
            if (lookupService != null)
                return lookupService;
            lookupService = GetLookupServiceForConnectionFor(fieldName, ObjectToEnter);
            if (lookupService != null)
                return lookupService;
            if (ParentService != null)
            {
                return ParentService.GetLookupService(reference + "." + fieldName, ObjectType.Name, ParentServiceReference, new ObjectRecord(ObjectToEnter));
            }
            return LookupService;
        }

        private IRecordService GetLookupServiceForConnectionFor(string fieldName, object objectToEnter)
        {
            var props = GetPropertyInfos(objectToEnter.GetType().Name);
            foreach (var prop in props)
            {
                var connectionsFor = prop.GetCustomAttributes<ConnectionFor>(true)
                    .Where(c => c.Property == fieldName);
                if (connectionsFor.Any())
                {
                    var value = objectToEnter.GetPropertyValue(prop.Name);
                    lock (_lockObject)
                    {
                        if (value != null)
                        {
                            if (_serviceConnections.ContainsKey(value))
                                return _serviceConnections[value];

                            var connectionFor = connectionsFor.First();
                            if (connectionFor is LookupConnectionFor)
                            {
                                if (!(value is Lookup))
                                    throw new Exception(string.Format("Value is required to be of type {0} for {1} attribute. Actual type is {2}. The property name is"
                                        , typeof(Lookup).Name, value.GetType().Name, prop.Name));
                                var lookup = (Lookup) value;
                                var lookupLookupService = GetLookupService(prop.Name, objectToEnter.GetType().Name, null,
                                    null);
                                var lookupConnectionFor = (LookupConnectionFor)connectionFor;
                                var parsedService = TypeLoader.LoadService(lookup, lookupLookupService, lookupConnectionFor, ObjectResolver);
                                _serviceConnections.Add(value, parsedService);
                                return parsedService;
                            }
                            
                            var serviceType = connectionFor.ServiceType;
                            var connectionFieldType = value.GetType();
                            if (serviceType == null)
                            {
                                var serviceConnectionAttr =
                                    connectionFieldType.GetCustomAttribute<ServiceConnection>(true);
                                if (serviceConnectionAttr == null)
                                    throw new NullReferenceException(
                                        string.Format(
                                            "The Property {0} Is Specified With A {1} Attribute However It's Type {2} Does Not Have The {3} Attribute Record To Create The {4}",
                                            prop.Name, typeof (ConnectionFor).Name, connectionFieldType.Name,
                                            typeof (ServiceConnection).Name, typeof (IRecordService).Name));
                                serviceType = serviceConnectionAttr.ServiceType;
                            }
                            var service = TypeLoader.LoadServiceForConnection(value, serviceType);
                            _serviceConnections.Add(value, service);
                            return service;
                        }
                    }
                }
            }
            return null;
        }

        public override object ParseField(string fieldName, string recordType, object value)
        {
            var fieldType = this.GetFieldType(fieldName, recordType);
            if (fieldType == RecordFieldType.Picklist)
            {
                if (value is PicklistOption)
                    value =
                        Enum.Parse(
                            GetPropertyType(fieldName, recordType),
                            ((PicklistOption)value).Key);
                return value;
            }
            if (fieldType == RecordFieldType.Integer && value is string)
            {
                if (((string)value).IsNullOrWhiteSpace())
                    return
                        this.GetFieldMetadata(fieldName, recordType).IsNonNullable
                            ? 0
                            : (int?)null;
                else
                    return int.Parse((string)value);
            }
            return base.ParseField(fieldName, recordType, value);
        }

        public IEnumerable<PropertyValidator> GetValidatorAttributes(string fieldName, string recordType)
        {
            return GetClassType(recordType).GetValidatorAttributes(fieldName);
        }

        public override IEnumerable<ViewMetadata> GetViews(string recordType)
        {
            //very similar logic in form get grid metadata
            var viewFields = new List<ViewField>();
            var type = GetClassType(recordType);
            foreach (var propertyInfo in type.GetProperties())
            {
                var hiddenAttribute = propertyInfo.GetCustomAttribute<HiddenAttribute>();
                if (propertyInfo.CanRead && hiddenAttribute == null)
                {
                    //these initial values repeated
                    var viewField = new ViewField(propertyInfo.Name, int.MaxValue, 200);
                    var orderAttribute = propertyInfo.GetCustomAttribute<DisplayOrder>();
                    if (orderAttribute != null)
                        viewField.Order = orderAttribute.Order;
                    var widthAttribute = propertyInfo.GetCustomAttribute<GridWidth>();
                    if (widthAttribute != null)
                        viewField.Width = widthAttribute.Width;
                    viewFields.Add(viewField);
                }
            }
            return new[] { new ViewMetadata(viewFields) { ViewType = ViewType.LookupView } };
        }
    }
}