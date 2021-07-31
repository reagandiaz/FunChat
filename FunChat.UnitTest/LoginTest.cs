﻿using FunChat.GrainIntefaces;
using FunChat.UnitTest.Tools;
using Orleans.TestingHost;
using System;
using System.Threading.Tasks;
using Xunit;


namespace FunChat.UnitTest
{
    [Collection(ClusterCollection.Name)]
    public class LoginTest
    {
        private readonly TestCluster _cluster;

        public LoginTest(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }

        internal async Task Login(string username, string password, bool isvalid)
        {
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            Guid guid = Guid.Empty;
            if (user != null)
                guid = await user.Login(username, password);
       
            if (isvalid)
                Assert.True(guid != Guid.Empty);
            else
                Assert.True(guid == Guid.Empty);
        }


        //I can login to FunChat using any(and only) alphanumeric characters as login name, the password is the same as the user’s login name.
        //Login name characters must be 3 ~ 10 characters

        [Fact]
        public async Task LoginAlpha2CharsInvalid()
        {
            await Login("ab", "ab", false);
        }

        [Fact]
        public async Task LoginAlpha3CharsValid()
        {
            await Login("abc", "abc", true);
        }

        [Fact]
        public async Task LoginAlpha10CharsValid()
        {
            await Login("abcdefghij", "abcdefghij", true);
        }

        [Fact]
        public async Task LoginAlpha11CharsInValid()
        {
            await Login("abcdefghijk", "abcdefghijk", false);
        }

        [Fact]
        public async Task LoginUserPasswordDifferent()
        {
            await Login("username", "password", false);
        }
        
        [Fact]
        public async Task LoginUserNotAlphanumeric()
        {
            await Login("user_name", "user_name", false);
        } 
    }
}