using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using SecureBank.Filters;
using Xunit;

namespace SecureBank.Tests.Filters
{
    public class SessionAuthorizeAttributeTests
    {
        private class TestSession : ISession
        {
            private readonly Dictionary<string, byte[]> _store = new();

            public bool IsAvailable => true;
            public string Id => "test-session-id";
            public IEnumerable<string> Keys => _store.Keys;

            public void Clear() => _store.Clear();

            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public void Remove(string key) => _store.Remove(key);

            public void Set(string key, byte[] value) => _store[key] = value;

            public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
        }

        private static ActionExecutingContext CreateActionContext(ISession session)
        {
            var httpContext = new DefaultHttpContext { Session = session };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());
        }

        [Fact]
        public void OnActionExecuting_WhenUserIdNotInSession_RedirectsToAuthLogin()
        {
            var session = new TestSession(); // No UserId set
            var context = CreateActionContext(session);
            var filter = new SessionAuthorizeAttribute();

            filter.OnActionExecuting(context);

            var redirectResult = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Auth", redirectResult.ControllerName);
        }

        [Fact]
        public void OnActionExecuting_WhenUserIdInSession_AllowsExecution()
        {
            var session = new TestSession();
            session.SetInt32("UserId", 42);
            var context = CreateActionContext(session);
            var filter = new SessionAuthorizeAttribute();

            filter.OnActionExecuting(context);

            Assert.Null(context.Result); // Execution allowed to proceed
        }
    }
}
