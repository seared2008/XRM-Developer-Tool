﻿#region

using JosephM.Application.Modules;
using JosephM.Application.ViewModel.Fakes;
using JosephM.Prism.Infrastructure.Constants;
using JosephM.Prism.Infrastructure.Module;
using JosephM.Record.Application.Fakes;

#endregion

namespace JosephM.Prism.TestModule.Prism
{
    public class TestModule : PrismModuleBase
    {
        public override void InitialiseModule()
        {
            AddOption("Create Test Record", CreateCommand);
        }

        public override void RegisterTypes()
        {
            RegisterTypeForNavigation<FakeMaintainViewModel>();
        }

        private void CreateCommand()
        {
            ApplicationController.OpenRecord(FakeConstants.RecordType, FakeConstants.Id, "",
                typeof(FakeMaintainViewModel));
        }
    }
}