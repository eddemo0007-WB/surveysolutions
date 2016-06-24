﻿using System;
using System.Threading.Tasks;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;

namespace WB.UI.Tester.Infrastructure.Internals.Security
{
    internal class TesterPrincipal : IPrincipal
    {
        private readonly IAsyncPlainStorage<TesterUserIdentity> usersStorage;
        private TesterUserIdentity currentUserIdentity;

        public bool IsAuthenticated => this.currentUserIdentity != null;
        public IUserIdentity CurrentUserIdentity => this.currentUserIdentity;

        public TesterPrincipal(IAsyncPlainStorage<TesterUserIdentity> usersStorage)
        {
            this.usersStorage = usersStorage;
            this.currentUserIdentity = usersStorage.FirstOrDefault();
        }

        public async Task<bool> SignInAsync(string userName, string password, bool staySignedIn)
        {
            this.currentUserIdentity = new TesterUserIdentity
            {
                Name = userName,
                Password = password,
                UserId = Guid.NewGuid(),
                Id = userName
            };

            if (staySignedIn)
            {
                await this.usersStorage.StoreAsync(this.currentUserIdentity).ConfigureAwait(false);
            }

            return this.IsAuthenticated;
        }

        public void SignOut()
        {
            this.currentUserIdentity = null;
        }
    }
}