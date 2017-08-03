﻿using System;
using WB.Core.BoundedContexts.Designer.MembershipProvider.Roles;

namespace WB.Core.BoundedContexts.Designer.Commands.Account
{
    [Serializable]
    public class AssignUserRole : UserCommand
    {
        public AssignUserRole(Guid userId, SimpleRoleEnum role)
            : base(userId)
        {
            this.Role = role;
        }

        public SimpleRoleEnum Role { get; private set; }
    }
}