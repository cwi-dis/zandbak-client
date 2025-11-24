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
        private Data.Bubble _bubbleData;

        public Data.Bubble BubbleData
        {
            set => _bubbleData = value;
        }

        public string Id => _bubbleData.Id;
        public string Name => _bubbleData.Name;
        public User Owner => Session.Users.Find((u) => u.Id == _bubbleData.Owner.Id);
        public List<User> Users => _bubbleData.Users.Select((bubbleUser) => Session.Users.Find((sessionUser) => sessionUser.Id == bubbleUser.Id)).ToList();

        public Session Session => _orchestrator.CurrentSession;

        /// <summary>
        /// Event triggered when a user joins the bubble.
        /// </summary>
        /// <remarks>
        /// This Action is invoked with the user who joined the bubble as a parameter.
        /// </remarks>
        public Action<User> OnUserJoined;

        /// <summary>
        /// Event triggered when a user leaves the bubble.
        /// </summary>
        /// <remarks>
        /// This Action is invoked with the user who left the bubble as a parameter.
        /// </remarks>
        public Action<User> OnUserLeft;

        /// <summary>
        /// Event triggered when a user requests to join the bubble.
        /// </summary>
        /// <remarks>
        /// This Action is invoked with the user who requested to join the bubble as a parameter.
        /// </remarks>
        public Action<User> OnJoinRequested;

        public Bubble(Orchestrator orchestrator, Data.Bubble bubbleData)
        {
            _orchestrator = orchestrator;
            _bubbleData = bubbleData;

            OrchestratorController.Instance.OnBubbleJoined += UserJoined;
            OrchestratorController.Instance.OnBubbleLeft += UserLeft;
            OrchestratorController.Instance.OnBubbleJoinRequested += JoinRequested;
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

            OrchestratorController.Instance.Wrapper.LeaveBubble((status, _) =>
            {
                if (status.Error == ResponseStatus.Ok)
                {
                    Session.CurrentBubble = null;
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
        /// Approves or denies a user's request to join the current bubble asynchronously by interacting with the Orchestrator.
        /// </summary>
        /// <param name="user">
        /// The user whose join request is being approved or denied.
        /// </param>
        /// <param name="approve">
        /// A boolean indicating whether to approve (true) or deny (false) the join request.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is true if the operation succeeds; otherwise, an exception is thrown in case of an error.
        /// </returns>
        public Task<bool> ApproveBubbleJoinRequest(User user, bool approve)
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.Wrapper.ApproveBubbleJoinRequest(user.Id, Id, approve, (status) =>
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

        private void UserJoined(Data.User user)
        {
            _bubbleData.Users.Add(user);
            OnUserJoined?.Invoke(Session.FindUserById(user.Id));
        }

        private void UserLeft(Data.User user)
        {
            _bubbleData.Users.Remove(user);
            OnUserLeft?.Invoke(Session.FindUserById(user.Id));
        }

        private void JoinRequested(Data.User user)
        {
            OnJoinRequested?.Invoke(Session.FindUserById(user.Id));
        }
    }
}
