using Rafty.Concensus;
using Xunit;
using Shouldly;
using TestStack.BDDfy;
using System;
using System.Collections.Generic;
using Rafty.Log;

namespace Rafty.UnitTests.obj
{
    public class FollowerAppendEntries
    {
/*
1. Reply false if term < currentTerm (§5.1)
2. Reply false if log doesn’t contain an entry at prevLogIndex
whose term matches prevLogTerm (§5.3)
3. If an existing entry conflicts with a new one (same index
but different terms), delete the existing entry and all that
follow it (§5.3)
4. Append any new entries not already in the log
5. If leaderCommit > commitIndex, set commitIndex =
min(leaderCommit, index of last new entry)*/

        private Node _node;
        private ISendToSelf _sendToSelf;
        private CurrentState _currentState;
        
        public FollowerAppendEntries()
        {
            _sendToSelf = new TestingSendToSelf();
            _currentState = new CurrentState(Guid.NewGuid(), new List<IPeer>(), 0, default(Guid), TimeSpan.FromSeconds(5), new InMemoryLog(), 0);
            _node = new Node(_currentState, _sendToSelf);
            _sendToSelf.SetNode(_node);
        }
        

        public void Dispose()
        {
            _node.Dispose();
        }

        
        [Fact(DisplayName = "AppendEntries - 1. Reply false if term < currentTerm (§5.1)")]
        public void ShouldReplyFalseIfRpcTermLessThanCurrentTerm()
        {
            _currentState = new CurrentState(Guid.NewGuid(), new List<IPeer>(), 1, default(Guid), TimeSpan.FromSeconds(5), new InMemoryLog(), 0);
            _node = new Node(_currentState, _sendToSelf);
            var appendEntriesRpc = new AppendEntriesBuilder().WithTerm(0).Build();
            var response = _node.Handle(appendEntriesRpc);
            response.Success.ShouldBe(false);
            response.Term.ShouldBe(1);
        }

        [Fact(DisplayName = "AppendEntries - 2. Reply false if log doesn’t contain an entry at prevLogIndex whose term matches prevLogTerm (§5.3)")]
        public void ShouldReplyFalseIfLogDoesntContainEntryAtPreviousLogIndexWhoseTermMatchesRpcPrevLogTerm()
        {
            _currentState = new CurrentState(Guid.NewGuid(), new List<IPeer>(), 2, default(Guid), TimeSpan.FromSeconds(5), new InMemoryLog(), 0);
            _currentState.Log.Apply(new LogEntry("", typeof(string), 2, 0));
            _node = new Node(_currentState, _sendToSelf);
            var appendEntriesRpc = new AppendEntriesBuilder().WithTerm(2).WithPreviousLogIndex(0).WithPreviousLogTerm(1).Build();
            var response = _node.Handle(appendEntriesRpc);
            response.Success.ShouldBe(false);
            response.Term.ShouldBe(2);
        }

        [Fact(DisplayName = "AppendEntries - 3. If an existing entry conflicts with a new one (same index but different terms), delete the existing entry and all that follow it(§5.3)")]
        public void ShouldDeleteExistingEntryIfItConflictsWithNewOne()
        {
            _currentState = new CurrentState(Guid.NewGuid(), new List<IPeer>(), 1, default(Guid), TimeSpan.FromSeconds(5), new InMemoryLog(), 2);
            _currentState.Log.Apply(new LogEntry("term 1 commit index 0", typeof(string), 1, 0));
            _currentState.Log.Apply(new LogEntry("term 1 commit index 1", typeof(string), 1, 1));
            _currentState.Log.Apply(new LogEntry("term 1 commit index 2", typeof(string), 1, 2));
            _node = new Node(_currentState, _sendToSelf);
            var appendEntriesRpc = new AppendEntriesBuilder()
                .WithEntry(new LogEntry("term 2 commit index 2", typeof(string),2,2))
                .WithTerm(2)
                .WithPreviousLogIndex(1)
                .WithPreviousLogTerm(1)
                .Build();
            var response = _node.Handle(appendEntriesRpc);
            response.Success.ShouldBe(true);
            response.Term.ShouldBe(2);
            _node.State.CurrentState.Log.GetTermAtIndex(2).ShouldBe(2);
        }
    }
}