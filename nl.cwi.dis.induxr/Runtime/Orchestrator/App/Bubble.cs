using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orchestrator.Data;
using Orchestrator.Wrapping;

namespace Orchestrator.App
{
    public class Bubble
    {
        private readonly Orchestrator _orchestrator;
        private readonly Data.Bubble _bubbleData;

        public string Id => _bubbleData.Id;
        public string Name => _bubbleData.Name;
        public User Owner => Session.Users.Find((u) => u.Id == _bubbleData.Owner.Id);
        public List<User> Users => _bubbleData.Users.Select((bubbleUser) => Session.Users.Find((sessionUser) => sessionUser.Id == bubbleUser.Id)).ToList();

        public Session Session => _orchestrator.CurrentSession;

        public Bubble(Orchestrator orchestrator, Data.Bubble bubbleData)
        {
            _orchestrator = orchestrator;
            _bubbleData = bubbleData;
        }

        /// <summary>
        /// Leaves the current bubble asynchronously by notifying the Orchestrator and processes the response.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is true if the operation succeeds; otherwise, an exception is thrown in case of an error.
        /// </returns>
        public Task<bool> LeaveBubble()
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.Wrapper.LeaveBubble(Id, (status, _) =>
            {
                if (status.Error == ResponseStatus.Ok)
                {
                    tcs.SetResult(true);
                }
                else
                {
                    tcs.SetException(new Exception(status.Message));
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Invites a user to the current bubble asynchronously by communicating with the Orchestrator.
        /// </summary>
        /// <param name="u">The user to be invited to the bubble.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is true if the invitation succeeds; otherwise, an exception is thrown in case of an error.
        /// </returns>
        public Task<bool> InviteUser(User u)
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.Wrapper.InviteToBubble(u.Id, (status) =>
            {
                if (status.Error == ResponseStatus.Ok)
                {
                    tcs.SetResult(true);
                }
                else
                {
                    tcs.SetException(new Exception(status.Message));
                }
            });

            return tcs.Task;
        }
    }
}
