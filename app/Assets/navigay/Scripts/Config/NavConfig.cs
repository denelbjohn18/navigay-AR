using UnityEngine;

namespace Navigay.Config
{
    public sealed class NavConfig : ScriptableObject
    {
        public float checkpoint_turn_deg = 30f;
        public float stuck_error_m = 25f;
        public float stuck_hold_s = 2f;
        public float full_error_m = 5f;
        public float full_heading_deg = 10f;
        public float short_error_m = 12f;
        public float short_heading_deg = 20f;
        public float confidence_up_s = 2f;
        public float photo_match_s = 5f;
        public float snap_search_m = 25f;
        public float snap_stick_better_m = 3f;
        public float snap_stick_s = 2f;
        public float snap_off_route_m = 8f;
        public float snap_heading_penalty_m = 8f;
        public float snap_heading_disagree_deg = 45f;
        public float precise_error_m = 1000f;
        public float on_route_m = 2f;
        public float lowered_deg = 60f;
        public float dark_brightness = 0.15f;
        public float countdown_back_m = 3f;
        public float checkpoint_reach_m = 8f;
        public float walk_speed_mps = 1.2f;
        public float walk_speed_min_mps = 0.8f;
        public float walk_speed_max_mps = 1.8f;
        public float reroute_off_m = 20f;
        public float reroute_moved_m = 10f;
        public float turn_reset_deg = 60f;
        public float turn_reset_s = 3f;
        public float turn_reset_m = 15f;
        public float dot_spacing_m = 1.5f;
        public float dot_ground_offset_m = 1.5f;
        public float full_dots_m = 40f;
        public float short_dots_m = 10f;
        public int max_dots = 30;

        public static NavConfig CreateDefault()
        {
            return CreateInstance<NavConfig>();
        }
    }
}
