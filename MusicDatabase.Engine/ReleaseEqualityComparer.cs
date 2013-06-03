using System;
using System.Collections.Generic;
using System.Linq;
using KellermanSoftware.CompareNetObjects;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    public class ReleaseEqualityComparer : IEqualityComparer<Release>
    {
        private CompareObjects compareObjects;

        public string DifferencesString { get; private set; }

        public ReleaseEqualityComparer(bool ignoreNonDataProperties)
        {
            this.compareObjects = new CompareObjects();
            this.compareObjects.IgnoreObjectTypes = true;
            this.compareObjects.EmptyStringEqualsNull = true;

            if (ignoreNonDataProperties)
            {
                this.compareObjects.ElementsToIgnore.Add("Id");
            }
        }

        public bool Equals(Release x, Release y)
        {
            return this.Equals(null, x, null, y);
        }

        public bool Equals(ICollectionManager xm, Release x, ICollectionManager ym, Release y)
        {
            if (!this.compareObjects.Compare(x, y))
            {
                this.DifferencesString = this.compareObjects.DifferencesString; ;
                return false;
            }

            if (xm != null && ym != null)
            {
                for (int i = 0; i < x.Images.Count; ++i)
                {
                    byte[] image = xm.ImageHandler.LoadImage(x.Images[i]);
                    byte[] importedImage = ym.ImageHandler.LoadImage(y.Images[i]);
                    if (!Enumerable.SequenceEqual(image, importedImage))
                    {
                        this.DifferencesString = "Image index " + i + " not equal in data";
                        return false;
                    }
                }
            }

            this.DifferencesString = null;
            return true;
        }

        public int GetHashCode(Release release)
        {
            return Utility.GetCombinedHashCode(release.Title, release.JoinedAlbumArtists);
        }
    }
}
