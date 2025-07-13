using System;
using System.Collections.Generic;
using HairCarePlus.Server.Domain.Entities;

namespace HairCarePlus.Server.Domain.ValueObjects
{
    public class ChatMessage : BaseEntity
    {
        public string Content { get; private set; }
        public string? TranslatedContent { get; private set; }
        public string? SourceLanguage { get; private set; }
        public string? TargetLanguage { get; private set; }
        public MessageType Type { get; private set; }
        public MessageStatus Status { get; private set; }
        public Guid SenderId { get; private set; }
        public Guid ReceiverId { get; private set; }
        public List<ChatAttachment> Attachments { get; private set; }
        public bool IsRead { get; private set; }
        public DateTime? ReadAt { get; private set; }

        private ChatMessage() : base()
        {
            Attachments = new List<ChatAttachment>();
        }

        public ChatMessage(
            Guid id,
            string content,
            MessageType type,
            MessageStatus status,
            Guid senderId,
            Guid receiverId,
            DateTime createdAt)
        {
            Id = id;
            Content = content;
            Type = type;
            Status = status;
            SenderId = senderId;
            ReceiverId = receiverId;
            CreatedAt = createdAt;
            Attachments = new List<ChatAttachment>();
        }

        public ChatMessage(
            string content,
            string? sourceLanguage,
            string? targetLanguage,
            MessageType type,
            Guid senderId,
            Guid receiverId) : this()
        {
            Content = content;
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
            Type = type;
            SenderId = senderId;
            ReceiverId = receiverId;
            Status = MessageStatus.Sent;
            IsRead = false;
        }

        public void AddAttachment(ChatAttachment attachment)
        {
            Attachments.Add(attachment);
            Update();
        }

        public void SetTranslation(string translatedContent)
        {
            TranslatedContent = translatedContent;
            Update();
        }

        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            Update();
        }

        public void UpdateStatus(MessageStatus status)
        {
            Status = status;
            Update();
        }
    }

    public class ChatAttachment : BaseEntity
    {
        public string Url { get; private set; }
        public string FileName { get; private set; }
        public string MimeType { get; private set; }
        public long FileSize { get; private set; }
        public AttachmentType Type { get; private set; }

        private ChatAttachment() : base() { }

        public ChatAttachment(
            string url,
            string fileName,
            string mimeType,
            long fileSize,
            AttachmentType type)
        {
            Url = url;
            FileName = fileName;
            MimeType = mimeType;
            FileSize = fileSize;
            Type = type;
        }
    }

    public enum MessageType
    {
        Text,
        Image,
        Video,
        File,
        System
    }

    public enum MessageStatus
    {
        Sending,
        Sent,
        Delivered,
        Read,
        Failed
    }

    public enum AttachmentType
    {
        Image,
        Video,
        Document,
        Voice,
        Other
    }
} 