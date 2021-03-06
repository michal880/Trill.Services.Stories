using System;
using System.Collections.Generic;
using System.Linq;
using Convey.CQRS.Commands;

namespace Trill.Services.Stories.Application.Commands
{
    public class SendStory : ICommand
    {
        public Guid Id { get; } = Guid.NewGuid();
        public long StoryId { get; }
        public Guid UserId { get; }
        public string Title { get; }
        public string Text { get; }
        public IEnumerable<string> Tags { get; }
        public DateTime? VisibleFrom { get; }
        public DateTime? VisibleTo { get; }
        public bool Highlighted { get; }

        public SendStory(long storyId, Guid userId, string title, string text, IEnumerable<string> tags,
            DateTime? visibleFrom = null, DateTime? visibleTo = null, bool highlighted = false)
        {
            StoryId = storyId;
            UserId = userId;
            Title = title;
            Text = text;
            Tags = tags ?? Enumerable.Empty<string>();
            VisibleFrom = visibleFrom;
            VisibleTo = visibleTo;
            Highlighted = highlighted;
        }
    }
}