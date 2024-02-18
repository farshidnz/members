using System.Collections.Generic;
using Newtonsoft.Json;

namespace SettingsAPI.Model.Rest
{
    public class MandrillEmailBodyRequest
    {
        [JsonProperty(PropertyName = "key")] public string Key { get; set; }

        [JsonProperty(PropertyName = "template_name")]
        public string TemplateName { get; set; }

        [JsonProperty(PropertyName = "template_content")]
        public List<TemplateContent> TemplateContent { get; set; }

        [JsonProperty(PropertyName = "message")]
        public Message Message { get; set; }

        [JsonProperty(PropertyName = "async")] public bool Async { get; set; }
    }

    public class TemplateContent
    {
        [JsonProperty(PropertyName = "name")] public string Name { get; set; }

        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }
    }

    public class Message
    {
        [JsonProperty(PropertyName = "html")] public string Html { get; set; }

        [JsonProperty(PropertyName = "text")] public string Text { get; set; }

        [JsonProperty(PropertyName = "subject")]
        public string Subject { get; set; }

        [JsonProperty(PropertyName = "from_email")]
        public string FromEmail { get; set; }

        [JsonProperty(PropertyName = "from_name")]
        public string FromName { get; set; }

        [JsonProperty(PropertyName = "to")] public List<To> To { get; set; }

        [JsonProperty(PropertyName = "headers")]
        public Header Headers { get; set; }

        [JsonProperty(PropertyName = "merge_vars")]
        public List<MergeVar> MergeVars { get; set; }

        [JsonProperty(PropertyName = "global_merge_vars")]
        public List<GlobalMergeVar> GlobalMergeVars { get; set; }

        [JsonProperty(PropertyName = "merge_language")]
        public string MergeLanguage { get; set; }
    }

    public class To
    {
        [JsonProperty(PropertyName = "email")] public string Email { get; set; }
        [JsonProperty(PropertyName = "name")] public string Name { get; set; }
        [JsonProperty(PropertyName = "type")] public string Type { get; set; }
    }

    public class Header
    {
        [JsonProperty(PropertyName = "Reply-To")]
        public string ReplyTo { get; set; }
    }

    public class GlobalMergeVar
    {
        [JsonProperty(PropertyName = "name")] public string Name { get; set; }

        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }
    }

    public class MergeVar
    {
        [JsonProperty(PropertyName = "rcpt")] public string Rcpt { get; set; }
        [JsonProperty(PropertyName = "vars")] public List<Var> Vars { get; set; }
    }

    public class Var
    {
        [JsonProperty(PropertyName = "name")] public string Name { get; set; }

        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }
    }
}