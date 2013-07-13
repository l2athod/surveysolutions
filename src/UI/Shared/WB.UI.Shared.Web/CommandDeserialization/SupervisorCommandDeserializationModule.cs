﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject.Modules;

namespace WB.UI.Shared.Web.CommandDeserialization
{
    public class SupervisorCommandDeserializationModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<ICommandDeserializer>().To<SupervisorCommandDeserializer>();
        }
    }
}
