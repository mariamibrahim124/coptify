namespace Coptify.Web.Models;

public class Video
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string EpisodeNumber { get; set; } = "";
    public string PublishedDate { get; set; } = "";
    public string Duration { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string VideoUrl { get; set; } = "";
    public bool Published { get; set; } = true;
    public int SortOrder { get; set; }
}

public class SiteContent
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Section { get; set; } = "";
    public string Value { get; set; } = "";
}

public class AdminUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
