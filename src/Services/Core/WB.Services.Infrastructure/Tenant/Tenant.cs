﻿using Newtonsoft.Json;

namespace WB.Services.Infrastructure.Tenant
{
    public class TenantInfo
    {
        [JsonConstructor]
        public TenantInfo(string baseUrl, TenantId id, string name, string workspace = "primary")
        {
            BaseUrl = baseUrl;
            Id = id;
            Name = name;
            Workspace = workspace;
        }

        public TenantInfo(string baseUrl, string tenantId, string name = "", string workspace = "primary")
            : this(baseUrl, new TenantId(tenantId), name, workspace)
        {
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        public string BaseUrl { get; set; }
        public TenantId Id { get; set; }
        public string Name { get; set; }
        public string Workspace { get; set; }

        protected bool Equals(TenantInfo other)
        {
            return string.Equals(BaseUrl, other.BaseUrl) && Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(TenantInfo)) return false;
            return Equals((TenantInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((BaseUrl != null ? BaseUrl.GetHashCode() : 0) * 397) ^ (Id != null ? Id.GetHashCode() : 0);
            }
        }
    }
}
