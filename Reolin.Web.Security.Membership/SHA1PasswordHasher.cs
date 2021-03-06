﻿using Reolin.Web.Security.Membership.Core;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Reolin.Web.Security.Membership
{
    public class SHA1PasswordHasher : IUserPasswordHasher
    {
        public byte[] ComputeHash(string password)
        {
            if (password == null)
            {
                throw new ArgumentException(nameof(password));
            }

            return new SHA1CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }
}