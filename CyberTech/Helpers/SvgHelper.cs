using System.Collections.Generic;

namespace CyberTech.Helpers
{
    public static class IconHelper
    {
        private static readonly Dictionary<string, string> AttributeIcons = new Dictionary<string, string>
        {
            // === LAPTOP/PC ATTRIBUTES ===
            ["cpu"] = "fas fa-microchip",
            ["ram"] = "fas fa-memory",
            ["ổ cứng"] = "fas fa-hdd",
            ["card đồ họa"] = "fas fa-tv",
            ["màn hình"] = "fas fa-desktop",
            ["hệ điều hành"] = "fab fa-windows",
            ["gpu"] = "fas fa-tv",
            ["vga"] = "fas fa-tv",
            ["ssd"] = "fas fa-save",
            ["hdd"] = "fas fa-hdd",
            ["storage"] = "fas fa-hdd",
            ["display"] = "fas fa-desktop",
            ["screen"] = "fas fa-desktop",
            ["os"] = "fab fa-windows",
            ["windows"] = "fab fa-windows",
            ["mainboard"] = "fas fa-th",
            ["motherboard"] = "fas fa-th",

            // === DATABASE ATTRIBUTES ===
            ["cảm biến"] = "fas fa-search",
            ["card đồ họa"] = "fas fa-tv",
            ["cổng kết nối"] = "fas fa-plug",
            ["công nghệ đồng bộ"] = "fas fa-sync",
            ["cpu"] = "fas fa-microchip",
            ["độ phân giải"] = "fas fa-expand",
            ["dpi"] = "fas fa-crosshairs",
            ["hệ điều hành"] = "fab fa-windows",
            ["kết nối"] = "fas fa-wifi",
            ["kích thước"] = "fas fa-ruler",
            ["led"] = "fas fa-lightbulb",
            ["ổ cứng"] = "fas fa-hdd",
            ["ram"] = "fas fa-memory",
            ["số nút"] = "fas fa-hand-pointer",
            ["tấm nền"] = "fas fa-tv",
            ["tần số quét"] = "fas fa-sync-alt",
            ["thời gian phản hồi"] = "fas fa-clock",
            ["thời lượng pin"] = "fas fa-battery-full",
            ["trọng lượng"] = "fas fa-weight-hanging",

            // === TAI NGHE ATTRIBUTES ===
            ["loại kết nối"] = "fas fa-plug",
            ["âm thanh"] = "fas fa-volume-up",
            ["microphone"] = "fas fa-microphone",
            ["noise cancelling"] = "fas fa-volume-mute",
            ["wireless"] = "fas fa-wifi",
            ["bluetooth"] = "fab fa-bluetooth",

            // === BÀN PHÍM ATTRIBUTES ===
            ["loại switch"] = "fas fa-keyboard",
            ["layout"] = "fas fa-th-large",
            ["keycap"] = "fas fa-square",
            ["rgb lighting"] = "fas fa-palette",
            ["macro"] = "fas fa-code",

            // === MÀN HÌNH ATTRIBUTES ===
            ["panel"] = "fas fa-tv",

            // === GHẾ/BÀN ATTRIBUTES ===
            ["chất liệu"] = "fas fa-couch",
            ["màu sắc"] = "fas fa-palette",
            ["điều chỉnh"] = "fas fa-sliders-h",
            ["bánh xe"] = "fas fa-circle",
            ["tựa lưng"] = "fas fa-chair",

            // === GENERAL ATTRIBUTES ===
            ["thương hiệu"] = "fas fa-tag",
            ["brand"] = "fas fa-tag",
            ["giá tiền"] = "fas fa-dollar-sign",
            ["price"] = "fas fa-dollar-sign",
            ["connection"] = "fas fa-wifi",
            ["led rgb"] = "fas fa-lightbulb",
            ["wireless"] = "fas fa-wifi",
            ["wired"] = "fas fa-plug",
            ["gaming"] = "fas fa-gamepad",
            ["office"] = "fas fa-briefcase",
            ["professional"] = "fas fa-briefcase"
        };

        public static string GetFontAwesomeClass(string key)
        {
            if (string.IsNullOrEmpty(key)) return "fas fa-cog";

            key = key.ToLower().Trim();
            var iconClass = AttributeIcons.ContainsKey(key) ? AttributeIcons[key] : "fas fa-cog";

            // Debug logging
            System.Diagnostics.Debug.WriteLine($"IconHelper: Key='{key}' -> Class='{iconClass}'");

            return iconClass;
        }

        // Backward compatibility
        public static string GetIconClass(string key) => GetFontAwesomeClass(key);
        public static string GetIconSvg(string key) => GetFontAwesomeClass(key);
    }

    // Backward compatibility
    public static class SvgHelper
    {
        public static string GetIconClass(string key) => IconHelper.GetFontAwesomeClass(key);
        public static string GetIconSvg(string key) => IconHelper.GetFontAwesomeClass(key);
    }

    public static class CategoryHelper
    {
        public static string GetIconForCategory(string categoryName)
        {
            switch (categoryName.ToLower())
            {
                case "laptop": return "fas fa-laptop";
                case "laptop gaming": return "fas fa-gamepad";
                case "pc gvn": return "fas fa-desktop";
                case "main, cpu, vga": return "fas fa-microchip";
                case "case, nguồn, tản": return "fas fa-server";
                case "ổ cứng, ram, thẻ nhớ": return "fas fa-memory";
                case "loa, micro, webcam": return "fas fa-volume-up";
                case "màn hình": return "fas fa-tv";
                case "bàn phím": return "fas fa-keyboard";
                case "chuột + lót chuột": return "fas fa-mouse";
                case "tai nghe": return "fas fa-headphones";
                case "ghế - bàn": return "fas fa-chair";
                case "phần mềm, mạng": return "fas fa-network-wired";
                case "handheld, console": return "fas fa-gamepad";
                case "phụ kiện (hub, sạc, cáp..)": return "fas fa-plug";
                case "dịch vụ và thông tin khác": return "fas fa-info-circle";
                default: return "fas fa-question";
            }
        }
    }
}