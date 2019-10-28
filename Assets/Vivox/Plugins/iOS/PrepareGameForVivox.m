#include <AVFoundation/AVFoundation.h>

void PrepareForVivox() {
    // Important: must set PlayAndRecord category for simultaneous input/output
    // Default to speaker will play from speakers instead of the receiver (ear speaker) when headphones are not used.
    [AVAudioSession.sharedInstance
     setCategory:AVAudioSessionCategoryPlayAndRecord
     withOptions:AVAudioSessionCategoryOptionDefaultToSpeaker
     error:nil];
    // 44,100 sample rate must be used for iOS
    [AVAudioSession.sharedInstance setPreferredSampleRate:44100 error:nil];
}
