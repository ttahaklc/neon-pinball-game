#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *gLight, *gMedium, *gHeavy;
static UINotificationFeedbackGenerator *gNotify;

extern "C" {
    void NeonHaptic_Init(void) {
        gLight = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        gMedium = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
        gHeavy = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
        gNotify = [[UINotificationFeedbackGenerator alloc] init];
        [gLight prepare]; [gMedium prepare]; [gHeavy prepare]; [gNotify prepare];
    }
    void NeonHaptic_Impact(int style) {
        UIImpactFeedbackGenerator *g = style == 0 ? gLight : (style == 1 ? gMedium : gHeavy);
        [g impactOccurred];
        [g prepare];
    }
    void NeonHaptic_Notify(int type) {
        [gNotify notificationOccurred:(type == 0 ? UINotificationFeedbackTypeSuccess : UINotificationFeedbackTypeError)];
        [gNotify prepare];
    }
}
