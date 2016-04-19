﻿namespace WB.UI.Shared.Web.Membership
{
    using System;
    using System.Web.Security;
  
    public class MembershipWebUser : IMembershipWebUser
    {
        private readonly IMembershipHelper hepler;

        public MembershipWebUser(IMembershipHelper helper)
        {
            this.hepler = helper;
        }

        public MembershipUser MembershipUser => Membership.GetUser();

        public Guid UserId => Guid.Parse(this.MembershipUser.ProviderUserKey.ToString());

        public string UserName => MembershipUser.UserName;

        public bool IsAdmin => Roles.IsUserInRole(this.hepler.ADMINROLENAME);
    }
}