namespace UnityEngine.Experimental.Rendering
{
    public static class TileLayoutUtils
    {
        public static bool TryLayoutByTiles(
            RectInt src,
            uint tileSize,
            out RectInt main,
            out RectInt topRow,
            out RectInt rightCol,
            out RectInt topRight)
        {
            if (src.width < tileSize || src.height < tileSize)
            {
                main = RectInt.zero;
                topRow = RectInt.zero;
                rightCol = RectInt.zero;
                topRight = RectInt.zero;
                return false;
            }

            int mainRows = src.height / (int)tileSize;
            int mainCols = src.width / (int)tileSize;
            int mainWidth = mainCols * (int)tileSize;
            int mainHeight = mainRows * (int)tileSize;

            main = new RectInt
            {
                x = src.x,
                y = src.y,
                width = mainWidth,
                height = mainHeight,
            };
            topRow = new RectInt
            {
                x = src.x,
                y = src.y + mainHeight,
                width = mainWidth,
                height = src.height - mainHeight
            };
            rightCol = new RectInt
            {
                x = src.x + mainWidth,
                y = src.y,
                width = src.width - mainWidth,
                height = mainHeight
            };
            topRight = new RectInt
            {
                x = src.x + mainWidth,
                y = src.y + mainHeight,
                width = src.width - mainWidth,
                height = src.height - mainHeight
            };

            return true;
        }

        public static bool TryLayoutByRow(
            RectInt src,
            uint tileSize,
            out RectInt main,
            out RectInt other)
        {
            if (src.height < tileSize)
            {
                main = RectInt.zero;
                other = RectInt.zero;
                return false;
            }

            int mainRows = src.height / (int)tileSize;
            int mainHeight = mainRows * (int)tileSize;

            main = new RectInt
            {
                x = src.x,
                y = src.y,
                width = src.width,
                height = mainHeight,
            };
            other = new RectInt
            {
                x = src.x,
                y = src.y + mainHeight,
                width = src.width,
                height = src.height - mainHeight
            };

            return true;
        }

        public static bool TryLayoutByCol(
            RectInt src,
            uint tileSize,
            out RectInt main,
            out RectInt other)
        {
            if (src.width < tileSize)
            {
                main = RectInt.zero;
                other = RectInt.zero;
                return false;
            }

            int mainCols = src.width / (int)tileSize;
            int mainWidth = mainCols * (int)tileSize;

            main = new RectInt
            {
                x = src.x,
                y = src.y,
                width = mainWidth,
                height = src.height,
            };
            other = new RectInt
            {
                x = src.x + mainWidth,
                y = src.y,
                width = src.width - mainWidth,
                height = src.height
            };

            return true;
        }
    }
}
