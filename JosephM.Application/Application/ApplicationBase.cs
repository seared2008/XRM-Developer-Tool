﻿using System;
using System.Collections.Generic;
using System.Reflection;
using JosephM.Application.Modules;
using JosephM.Application.Options;
using JosephM.Core.AppConfig;
using JosephM.Core.Extentions;

namespace JosephM.Application.Application
{
    public class ApplicationBase
    {
        public ApplicationControllerBase Controller { get; set; }

        public ApplicationBase(ApplicationControllerBase applicationController, IApplicationOptions applicationOptions)
        {
            Modules = new Dictionary<Type, ModuleBase>();
            Controller = applicationController;
            Controller.RegisterInfrastructure(applicationOptions);
        }

        public ApplicationBase(ApplicationControllerBase applicationController)
            : this(applicationController, new ApplicationOptions())
        {
        }

        private IDictionary<Type, ModuleBase> Modules { get; set; }

        public void AddModule<T>()
            where T : ModuleBase, new()
        {
            AddModule(typeof (T));
        }

        public T GetModule<T>()
            where T : ModuleBase, new()
        {
            if(!Modules.ContainsKey(typeof(T)))
                throw new NullReferenceException(string.Format("Type {0} is not loaded as a module", typeof(T).Name));
            return (T)Modules[typeof (T)];
        }

        private void AddModule(Type moduleType)
        {
            if (Modules.ContainsKey(moduleType))
                return;

            var moduleController = Controller.ResolveType<ModuleController>();

            var dependantModuleAttributes =
                moduleType.GetCustomAttributes<DependantModuleAttribute>();
            foreach (var dependantModule in dependantModuleAttributes)
                AddModule(dependantModule.DependantModule);

            //okay it needs to add items to the container
            if (!moduleType.IsTypeOf(typeof (ModuleBase)))
                throw new NullReferenceException(string.Format("Object type {0} is not of type {1}", moduleType.Name,
                    typeof (ModuleBase).Name));
            if (!moduleType.HasParameterlessConstructor())
                throw new NullReferenceException(
                    string.Format("Object type {0} does not have a parameterless constructor", moduleType.Name));

            var theModule = (ModuleBase) moduleType.CreateFromParameterlessConstructor();
            theModule.Controller = moduleController;
            theModule.RegisterTypes();
            theModule.InitialiseModule();
            Modules.Add(moduleType, theModule);
        }

        protected void LogError(string message)
        {
            Controller.UserMessage(message);
        }
    }
}