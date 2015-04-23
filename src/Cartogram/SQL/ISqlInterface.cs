using System.Collections.Generic;
using System.Data;

namespace Cartogram.SQL
{
    internal interface ISqlInterface
    {

        int AddMap(Map newMap);

        Map GetMap(int mapId);

        DataTable MapDataTable();

        DataTable DropDataTable(int mapId);

        void AddDrop(Map newMap, int mapid, int zana = 0, int carto = 0);

        int MapDrops(int mapId, string symbol);

        long ExpGained(int id);

        void AddCurrency(int mapId, KeyValuePair<int, string> currency);

        void AddUnique(int mapId, string name);

        List<KeyValuePair<int, string>> MapList(int id);

        List<string> MapAffixes(int id);

        void FinishMap(int id, Experience exp);

        bool DeleteMap(int id);

        void UpdateNotes(int id, string notes);

        void AddExperience(List<Experience> expList);

        int ExperienceCount();

        int ExperienceGoal(int level);

    }
}
