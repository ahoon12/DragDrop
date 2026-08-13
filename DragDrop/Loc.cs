using UnityEngine;

namespace DragDrop
{
    public static class Loc
    {
        public static string T(string ko, string en)
        {
            if (RDString.language == SystemLanguage.Korean)
                return ko;
            return en;
        }
    }
}
