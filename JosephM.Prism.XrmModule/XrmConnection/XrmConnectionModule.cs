﻿#region

using System.Configuration;
using JosephM.Application.Application;
using JosephM.Application.Modules;
using JosephM.Core.AppConfig;
using JosephM.Core.Extentions;
using JosephM.Core.Log;
using JosephM.Prism.Infrastructure.Constants;
using JosephM.Prism.Infrastructure.Module;
using JosephM.Prism.Infrastructure.Prism;
using JosephM.Prism.XrmModule.Xrm;
using JosephM.Record.Xrm.XrmRecord;

#endregion

namespace JosephM.Prism.XrmModule.XrmConnection
{
    public class XrmConnectionModule : PrismModuleBase
    {
        public override void RegisterTypes()
        {
            var configManager = Resolve<PrismSettingsManager>();

            try
            {
                var xrmConfiguration = configManager.Resolve<XrmRecordConfiguration>();
                RefreshXrmServices(xrmConfiguration, ApplicationController);
            }
            catch (ConfigurationErrorsException ex)
            {
                ApplicationController.UserMessage(
                    string.Concat("Warning!! There was an error reading the crm connection from config\n",
                        ex.DisplayString()));
            }

            RegisterTypeForNavigation<XrmMaintainViewModel>();
            RegisterTypeForNavigation<XrmCreateViewModel>();
            RegisterTypeForNavigation<XrmConnectionDialog>();
            //RegisterType<XrmFormController>();
            //RegisterType<XrmFormService>();
        }

        public static void RefreshXrmServices(IXrmRecordConfiguration xrmConfiguration, IApplicationController controller)
        {
            controller.RegisterInstance<IXrmRecordConfiguration>(xrmConfiguration);
            controller.RegisterInstance(new XrmRecordService(xrmConfiguration, controller.ResolveType<LogController>()));
        }

        public override void InitialiseModule()
        {
            AddSetting("Connect To Crm", ConnectToCrm);
            AddHelp("Connect To Crm", "Connect To CRM Help.htm");
        }

        private void ConnectToCrm()
        {
            NavigateTo<XrmConnectionDialog>();
        }
    }
}