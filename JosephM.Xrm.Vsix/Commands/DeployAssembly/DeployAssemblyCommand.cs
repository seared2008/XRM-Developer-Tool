﻿//------------------------------------------------------------------------------
// <copyright file="Command1.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using JosephM.XRM.VSIX.Dialogs;
using JosephM.XRM.VSIX.Utilities;

namespace JosephM.XRM.VSIX.Commands.DeployAssembly
{
    internal sealed class DeployAssemblyCommand : CommandBase
    {
        public override int CommandId
        {
            get { return 0x0103; }
        }

        private DeployAssemblyCommand(XrmPackage package)
            : base(package)
        {
        }

        public static DeployAssemblyCommand Instance { get; private set; }


        public static void Initialize(XrmPackage package)
        {
            Instance = new DeployAssemblyCommand(package);
        }

        public override void MenuItemCallback(object sender, EventArgs e)
        {
            var assemblyFile = VsixUtility.BuildSelectedProjectAndGetAssemblyName(ServiceProvider);
            if (!string.IsNullOrWhiteSpace(assemblyFile))
            {
                var settings = VsixUtility.GetPackageSettings(GetDte2());
                if (settings == null)
                    settings = new XrmPackageSettings();
                var dialog = new DeployAssemblyDialog(DialogUtility.CreateDialogController(), assemblyFile,
                    GetXrmRecordService(), settings);
                DialogUtility.LoadDialog(dialog);
            }
        }
    }
}
