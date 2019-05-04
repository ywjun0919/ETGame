using System.Collections.Generic;
using System.ComponentModel
;namespace ETModel
{
    public partial class UnitConfigCollection: ISupportInitialize
    {
        public Dictionary<long, UnitConfig> configDict = new Dictionary<long, UnitConfig>();

        public void BeginInit()
        {
        }

        public void EndInit()
        {
            foreach (UnitConfig config in this.Configs)
            {
                this.configDict.Add(config.Id, config);
            }
        }
        public UnitConfig Get(long id)
        {
           this.configDict.TryGetValue(id, out UnitConfig unitConfig);
           return unitConfig;
        }
    }
}
