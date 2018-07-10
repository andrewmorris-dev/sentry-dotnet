using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class DefaultUserFactoryTests
    {
        public HttpContext HttpContext { get; set; } = Substitute.For<HttpContext>();
        public IIdentity Identity { get; set; } = Substitute.For<IIdentity>();
        public ClaimsPrincipal User { get; set; } = Substitute.For<ClaimsPrincipal>();
        public ConnectionInfo ConnectionInfo { get; set; } = Substitute.For<ConnectionInfo>();
        public List<Claim> Claims { get; set; }

        public DefaultUserFactoryTests()
        {
            const string username = "test-user";
            Identity.Name.Returns(username); // by default reads: ClaimTypes.Name
            Claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, username +"@sentry.io"),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, "927391237"),
            };

            ConnectionInfo.RemoteIpAddress.Returns(IPAddress.IPv6Loopback);

            User.Identity.Returns(Identity);
            HttpContext.User.Returns(User);
            HttpContext.User.Claims.Returns(Claims);
            HttpContext.Connection.Returns(ConnectionInfo);
        }

        private readonly DefaultUserFactory _sut = new DefaultUserFactory();

        [Fact]
        public void Create_Fixture_CreatesCompleteUser()
        {
            var actual = _sut.Create(HttpContext);

            Assert.NotNull(actual);
            Assert.Equal(Claims.NameIdentifier(), actual.Id);
            Assert.Equal(Claims.Name(), actual.Username);
            Assert.Equal(Identity.Name, actual.Username);
            Assert.Equal(Claims.Email(), actual.Email);
            Assert.Equal(IPAddress.IPv6Loopback.ToString(), actual.IpAddress);
        }

        [Fact]
        public void Create_NoUser_Null()
        {
            HttpContext.User.ReturnsNull();
            Assert.Null(_sut.Create(HttpContext));
        }

        [Fact]
        public void Create_NoClaimsNoIdentityNoIpAddress_Null()
        {
            HttpContext.User.Identity.ReturnsNull();
            HttpContext.User.Claims.Returns(Enumerable.Empty<Claim>());
            HttpContext.Connection.ReturnsNull();
            Assert.Null(_sut.Create(HttpContext));
        }

        [Fact]
        public void Create_NoClaimsNoIdentity_Null()
        {
            HttpContext.User.Identity.ReturnsNull();
            HttpContext.User.Claims.Returns(Enumerable.Empty<Claim>());
            Assert.Null(_sut.Create(HttpContext));
        }

        [Fact]
        public void Create_NoClaims_UsernameFromIdentity()
        {
            HttpContext.User.Claims.Returns(Enumerable.Empty<Claim>());
            var actual = _sut.Create(HttpContext);
            Assert.Equal(Identity.Name, actual.Username);
        }

        [Fact]
        public void Create_NoClaims_IpAddress()
        {
            HttpContext.User.Claims.Returns(Enumerable.Empty<Claim>());
            var actual = _sut.Create(HttpContext);
            Assert.Equal(IPAddress.IPv6Loopback.ToString(), actual.IpAddress);
        }

        [Fact]
        public void Create_ClaimNameAndIdentityDontMatch_UsernameFromIdentity()
        {
            const string expected = "App configured to read it from a different claim";
            User.Identity.Name.Returns(expected);
            var actual = _sut.Create(HttpContext);

            Assert.Equal(expected, actual.Username);
        }

        [Fact]
        public void Create_Id_FromClaims()
        {
            Claims.RemoveAll(p => p.Type != ClaimTypes.NameIdentifier);
            var actual = _sut.Create(HttpContext);
            Assert.Equal(Claims.NameIdentifier(), actual.Id);
        }

        [Fact]
        public void Create_Username_FromClaims()
        {
            Claims.RemoveAll(p => p.Type != ClaimTypes.Name);
            var actual = _sut.Create(HttpContext);
            Assert.Equal(Claims.Name(), actual.Username);
        }

        [Fact]
        public void Create_Username_FromIdentity()
        {
            Claims.RemoveAll(p => p.Type != ClaimTypes.Name);
            var actual = _sut.Create(HttpContext);
            Assert.Equal(Identity.Name, actual.Username);
        }

        [Fact]
        public void Create_Email_FromClaims()
        {
            Claims.RemoveAll(p => p.Type != ClaimTypes.Email);
            var actual = _sut.Create(HttpContext);
            Assert.Equal(Claims.Email(), actual.Email);
        }

        [Fact]
        public void Create_NoRemoteIpAddress_NoIpAvailable()
        {
            ConnectionInfo.RemoteIpAddress.Returns(null as IPAddress);
            var actual = _sut.Create(HttpContext);
            Assert.Null(actual.IpAddress);
        }

        [Fact]
        public void Create_IpAddress_FromConnectionRemote()
        {
            var actual = _sut.Create(HttpContext);
            Assert.Equal(IPAddress.IPv6Loopback.ToString(), actual.IpAddress);
        }
    }
}