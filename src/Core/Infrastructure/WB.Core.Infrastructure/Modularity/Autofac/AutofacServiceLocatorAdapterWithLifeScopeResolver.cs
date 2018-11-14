﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Core.Lifetime;
//using WB.Core.GenericSubdomains.Portable.ServiceLocation;

namespace WB.Core.Infrastructure.Modularity.Autofac
{
    public class AutofacServiceLocatorAdapterWithLifeScopeResolver : AutofacServiceLocatorAdapter
    {
        //protected readonly ILifetimeScope rootScope;
        protected AsyncLocal<List<ILifetimeScope>> containers = new AsyncLocal<List<ILifetimeScope>>();

        public AutofacServiceLocatorAdapterWithLifeScopeResolver(ILifetimeScope rootScope):base(rootScope)
        {
            //this.rootScope = rootScope;
            this.rootScope.ChildLifetimeScopeBeginning += Scope_ChildLifetimeScopeBeginning;
            this.rootScope.CurrentScopeEnding += Scope_OnCurrentScopeEnding;
        }

        private void Scope_OnCurrentScopeEnding(object sender, LifetimeScopeEndingEventArgs e)
        {
            e.LifetimeScope.ChildLifetimeScopeBeginning -= Scope_ChildLifetimeScopeBeginning;
            e.LifetimeScope.CurrentScopeEnding -= Scope_OnCurrentScopeEnding;

            containers.Value?.RemoveAll(s => s.Equals(e.LifetimeScope));
        }

        private void Scope_ChildLifetimeScopeBeginning(object sender, LifetimeScopeBeginningEventArgs e)
        {
            if (containers.Value == null)
                containers.Value = new List<ILifetimeScope>();
            containers.Value.Add(e.LifetimeScope);

            e.LifetimeScope.ChildLifetimeScopeBeginning += Scope_ChildLifetimeScopeBeginning;
            e.LifetimeScope.CurrentScopeEnding += Scope_OnCurrentScopeEnding;
        }

        protected override object DoGetInstance(Type serviceType, string key)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            var container = GetCurrentScope();
            return key != null ? container.ResolveNamed(key, serviceType) : container.Resolve(serviceType);
        }

        protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            var container = GetCurrentScope();
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
            object instance = container.Resolve(enumerableType);
            return ((IEnumerable)instance).Cast<object>();
        }

        public ILifetimeScope GetCurrentScope()
        {
            return containers.Value?.LastOrDefault() ?? rootScope;
        }

        public ILifetimeScope GetRootScope()
        {
            return rootScope;
        }

        public bool IsRegistered(Type serviceType)
        {
            return GetCurrentScope().IsRegistered(serviceType);
        }
    }
}
