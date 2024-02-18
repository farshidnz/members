using System.Collections.Generic;

namespace SettingsAPI.Model.Dto
{
    public class Paging<T>
    {
        public Paging(List<T> data)
        {
            this.Data = data;
        }

        public List<T> Data { get; set; }
    }
}