namespace Entities.LinkModels
{
    public class LinkResourceBase //kaynaga ait linklerin organizasyonunu yapacak
    {
        public LinkResourceBase()
        {
            
        }

        public List<Link> Links { get; set; } = new List<Link>();
    }
}
