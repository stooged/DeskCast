//cast.receiver.logger.setLevelValue(0);

//============= Episode Metadata ==========
function EpisodeMetadata() {}

EpisodeMetadata.prototype = {    
    metadataSet: false,
    mediaContentType: "",
    mediaContentId: "",
    mediaTitle: "",
    mediaSubtitle: "",
    mediaImageUrl: "unknown.png"
};

//================= CC Environment ===========

function CCEnvironment() {
    this.receiver = new cast.receiver.Receiver('$RECEIVER$NAME$', [cast.receiver.RemoteMedia.NAMESPACE], "", 5);
    this.remoteMedia = new cast.receiver.RemoteMedia();
    
    this.remoteMedia.addChannelFactory(this.receiver.createChannelFactory(cast.receiver.RemoteMedia.NAMESPACE));

    this.remoteMedia.addEventListener(
       cast.receiver.Channel.EventType.CLOSED, this.onSenderDisconnected.bind(this));

    this.receiver.start();
}

CCEnvironment.prototype = {    
    
    _Player: null,

    init: function(player) {

        this._Player = player;
        this.remoteMedia.setMediaElement(player);
    },
    
    hasClients: function() {
        return this.remoteMedia.getChannels().length > 0;
    },
    
    broadcastFeedbackMessage: function (msg) {
        if (window.feedbackMessageBus) {
            window.feedbackMessageBus.broadcast(msg);
        }
    },
    
    onSenderDisconnected: function (event) {
        
        if (this.remoteMedia.getChannels().length == 0) {
            console.log("No more clients - pausing playback");
            this._Player.pause();
        }
    },
    
    getEpisodeMetadata: function() {

        var md = new EpisodeMetadata();

        if (this.remoteMedia.getTitle() != null) {

            var parts = this.remoteMedia.getTitle().split("#|#");
            if (parts.length > 0) {
                md.mediaTitle = parts[0];
            }

            if (parts.length > 2) {
                md.mediaContentType = parts[2];
            }

            if (parts.length > 1) {
                md.mediaSubtitle = parts[1];
            }
        }

        if (this.remoteMedia.getImageUrl() != null && this.remoteMedia.getImageUrl().length > 10)
            md.mediaImageUrl = this.remoteMedia.getImageUrl();

        return md;
    },
    
    stop: function() {
        this.receiver.stop();
    }
};


//============== BPApplication =============
function BPApplication(env, ui) {

    this._CCEnv = env;
    this._BPUI = ui;

}

BPApplication.prototype = {

    _BPUI: null,
    _CCEnv: null,
    _videoPlayer: null,
    _playbackError: false,
    _forceIdle: false,
    _idleTimeoutID: null,
    _exitTimeoutID: null,
    _screensaverTimeoutID: null,
    _loadingFrames: 0,
    _videoFramesRendered: false,
    _episodeMetadata: null,

    init: function () {

        this._BPUI.init();
        this._videoPlayer = this._BPUI.getPlayer();
        this._CCEnv.init(this._videoPlayer);
        
        this._playbackError = false;

        this._videoPlayer.addEventListener("canplay", this.onCanPlay.bind(this));
        this._videoPlayer.addEventListener("pause", this.onPause.bind(this));
        this._videoPlayer.addEventListener("ended", this.onEnded.bind(this));
        this._videoPlayer.addEventListener("loadstart", this.onLoadStart.bind(this));
        this._videoPlayer.addEventListener("seeking", this.onSeeking.bind(this));
        this._videoPlayer.addEventListener("seeked", this.onSeeked.bind(this));
        this._videoPlayer.addEventListener("error", this.onError.bind(this));
        this._videoPlayer.addEventListener("play", this.onPaly.bind(this));
        this._videoPlayer.addEventListener("playing", this.onPlaying.bind(this));
        this._videoPlayer.addEventListener("waiting", this.onWaiting.bind(this));
        this._videoPlayer.addEventListener("timeupdate", this.onTimeUpdate.bind(this));

        this.updateStatus();
        setInterval(this.updateStatus.bind(this), 1000);
        this.goToIdleDelayed(PAUSE_IDLE_DELAY);
    },

    isContentAudio : function() {
        return this._videoPlayer.videoHeight <= 0 || this._videoPlayer.videoWidth <= 0;
    },

    // Player handling events

    onSeeking : function() {
        this.logMessage("seeking");
        this.clearIdle();
        this.toggleBufferingIndicator(true);
        this.updateStatus();
        this.clearScreensaver();
    },
    
    onSeeked :  function () {
        this.logMessage("seeked! Is Paused: " + this._videoPlayer.paused);

        if (this._videoPlayer.paused) {
            this.togglePlayPauseIndicator(true);
        } else {
            if (this.isContentAudio)
                this.gototScrensaverDelayed(SCREENSAVER_DELAY);
        }

        this._loadingFrames = 0;

        this._CCEnv.broadcastFeedbackMessage({ event: 'seekCompleted' });

        this.goToIdleDelayed(PAUSE_IDLE_DELAY);
        this._playbackError = false;
        this.updateStatus();
    },
    
    onError: function() {
        this.logMessage("error");
        this._playbackError = true;
        this.toggleErrorIndicator(true);
        this.updateStatus();

        this.clearScreensaver();
        this.goToIdleDelayed(PAUSE_IDLE_DELAY);
        this._CCEnv.broadcastFeedbackMessage({ event: 'playbackError' });
    },

    onEnded : function () {
        this.logMessage("ended! Player source:" + this._videoPlayer.src);
        this.goToIdleDelayed(PLAYBACK_COMPLETE_IDLE_DELAY);

        this._CCEnv.broadcastFeedbackMessage(
            {
                event: 'mediaEnded',
                contentId: this._episodeMetadata.mediaContentId
            });
        
        this.clearScreensaver();
    },
    
    onPaly :  function () {
        this.logMessage("play");
        this.goToIdleDelayed(PAUSE_IDLE_DELAY);
    },
    
    onPlaying:  function () {
        this.logMessage("playing");
        this._loadingFrames = 0;
        this._playbackError = false;
        this.toggleBufferingIndicator(true);
        this.clearIdle();
        this.updateStatus();

        if (this.isContentAudio)
            this.gototScrensaverDelayed(SCREENSAVER_DELAY);
    },

    onCanPlay: function() {
        this.logMessage("canPlay");
        this.toggleBufferingIndicator(false);
        this.updateStatus();
    },
    
    onWaiting:  function () {
        this.logMessage("waiting");
        this.togglePlayPauseIndicator(true);
        this._loadingFrames = 0;
        this.updateStatus();
    },
    
    onTimeUpdate : function () {
        //logMessage(".");

        if (this._loadingFrames > 1) {
            this._videoFramesRendered = true;
            this.togglePlayPauseIndicator(false);
            this._loadingFrames = -1;
        } else if (this._loadingFrames != -1) {
            this._loadingFrames++;
        }
    },

    onPause: function() {

        this.logMessage("pause! Player source:" + this._videoPlayer.src);
        this.togglePlayPauseIndicator(true);
        this.goToIdleDelayed(PAUSE_IDLE_DELAY);
        this.clearScreensaver();
    },

    onLoadStart : function() {
        
        this.logMessage("loadstart for:" + this._videoPlayer.src);
        this.loadEpisodeMetadata();
        this._playbackError = false;
        this.goToIdleDelayed(PAUSE_IDLE_DELAY);
        this.toggleBufferingIndicator(true);
        this.updateStatus();
        this.clearScreensaver();
    },

    // UI management
    loadEpisodeMetadata: function () {

        this._episodeMetadata = window.DEMOMetadata ? window.DEMOMetadata : this._CCEnv.getEpisodeMetadata();

        /*this.logMessage("Loaded media: " + this._episodeMetadata.mediaTitle + ", " +
            this._episodeMetadata.mediaSubtitle + ", " +
            this._episodeMetadata.mediaContentType + ", " +
            this._episodeMetadata.mediaImageUrl);*/

        this._videoFramesRendered = false;

        this._BPUI.loadEpisodeMetadata(this._episodeMetadata);
    },

    pos2Time: function (rawSec) {

        var sec = Math.floor(rawSec);
        var hr = Math.floor(sec / 3600);
        var min = Math.floor((sec - (hr * 3600)) / 60);
        sec -= ((hr * 3600) + (min * 60));
        sec = Math.round(sec);

        sec = (hr > 0 || min > 0) && sec < 10 ? '0' + sec : sec;
        min = (min) ? ((hr > 0 && min < 10 ? '0' + min : min) + ":") : (hr > 0 ? '00:' : '');
        hr = (hr) ? (hr + ":") : '';

        return hr + min + sec + (hr.length == 0 && min.length == 0 ? 's' : '');
    },

    logMessage: function (text) {

        console.log(text);
    },

    toggleBufferingIndicator: function (turnOn) {
        this.toggleIndicator(turnOn ? 1 : 0);
    },

    togglePlayPauseIndicator: function (turnOn) {
        this.toggleIndicator(turnOn ? 2 : 0);
    },

    toggleErrorIndicator: function (turnOn) {
        this.toggleIndicator(turnOn ? 3 : 0);
    },

    toggleIndicator: function (indicator) {
        this._BPUI.togglePPEBIndicator(indicator);
    },

    updateStatus: function () {

        //Playback position and duration
        var dur = this._videoPlayer.duration;
        var pos = this._videoPlayer.currentTime;
        var progress = 0;
        var sRem = '', sPos = '';

        if (!isNaN(dur) && dur > 0) {
            progress = (pos / dur) * 100;
            sPos = this.pos2Time(pos);
            sRem = this.pos2Time(dur - pos);
        }

        // Idle vs. playback
        var showIdle = true;

        if (this._episodeMetadata == null || this._forceIdle) {
            showIdle = true;
        } else {
            showIdle = false;
        }

        //Error info
        var errorInfo = null;
        if (this._playbackError) {
            errorInfo = "Playback Error!";
        }

        if (!this._CCEnv.hasClients()) {
            errorInfo = "DeskCast has disconnected";
        }

        // Audio vs. video, track info
        var isAudio = false, showTrackInfo = false;
        if (!this._videoFramesRendered || this.isContentAudio()) {
            showTrackInfo = true;
            isAudio = true;
        } else {
            isAudio = false;

            if (this._videoPlayer.paused || this._videoPlayer.seeking || this._playbackError) {
                showTrackInfo = true;
            } else {
                showTrackInfo = false;
            }
        }

        this._BPUI.updateUI(sRem, sPos, progress, showIdle, isAudio, showTrackInfo, errorInfo);
    },

    //Timeout management
    goToIdleDelayed: function (timeout) {

        if (this._idleTimeoutID != null) {
            this.logMessage("Canceling old idle timeout...");
            clearTimeout(this._idleTimeoutID);
        }

        this.logMessage("Switching to idle in " + (timeout > 60000 ? (timeout / 60000) + " min" : (timeout / 1000) + " sec") + "...");
        this._idleTimeoutID = setTimeout(this.goToIdle.bind(this), timeout);
    },

    goToIdle: function () {

        if (this._idleTimeoutID != null)
            clearTimeout(this._idleTimeoutID);

        if (this._videoPlayer.paused || this._videoPlayer.ended || this._playbackError) {
            this.logMessage("Switching to idle...");
            this._forceIdle = true;
            this.updateStatus();
            this.closeApplicationDelayed(APP_CLOSE_DELAY);
        } else {
            this.logMessage("Switching to idle canceled! Player is not paused, ended or error-ed!");
        }
    },

    clearIdle: function () {

        if (this._idleTimeoutID != null) {
            this.logMessage("Canceling switch to idle!");
            clearTimeout(this._idleTimeoutID);
            this._idleTimeoutID = null;
        }

        if (this._exitTimeoutID != null) {
            this.logMessage("Canceling app close!");
            clearTimeout(this._exitTimeoutID);
            this._exitTimeoutID = null;
        }

        this._forceIdle = false;
        this.updateStatus();
    },

    closeApplicationDelayed: function (timeout) {

        if (this._exitTimeoutID != null)
            clearTimeout(this._exitTimeoutID);

        this.logMessage("Closing application in " + (timeout / 60000) + " min...");
        this._exitTimeoutID = setTimeout(this.closeApplication.bind(this), timeout);
    },

    closeApplication: function () {

        this._CCEnv.stop();
        window.close();
    },
    
    //Screensaver management
    gototScrensaverDelayed: function(timeout) {
        
        if (this._screensaverTimeoutID != null) {
            this.logMessage("Canceling old screensaver timeout...");
            clearTimeout(this._screensaverTimeoutID);
        }

        this.logMessage("Switching screensaver in " + (timeout > 60000 ? (timeout / 60000) + " min" : (timeout / 1000) + " sec") + "...");
        this._screensaverTimeoutID = setTimeout(this.goToScreensaver.bind(this), timeout);
    },
    
    goToScreensaver: function() {
        if (this._screensaverTimeoutID != null)
            clearTimeout(this._screensaverTimeoutID);

        if (!this._videoPlayer.paused && !this._videoPlayer.ended && this.isContentAudio()) {
            this.logMessage("Switching screensaver ON...");
            this._BPUI.toggleScreensaver(true);
        } else {
            this.logMessage("Switching to screensaver cacelled!");
        }
    },
    
    clearScreensaver: function () {
        this.logMessage("Switching screensaver OFF...");
        this._BPUI.toggleScreensaver(false);
    }
};

//================= UI Environment ===========
function BPUI() { }

BPUI.prototype = {

    _videoPlayer: null,
    _episodeMetadata: null,
    _overlayAlbumArt: null,
    _fullScreenAlbumArt: null,
    _episodeTitle: '',
    _episodeDescription: '',
    _playPosition: '',
    _playDuration: '',
    _playbackProgress: null,
    _playerContainer: null,
    _idleScreen: null,
    _playbackOverlay: null,
    _Screensaver: null,

    init: function() {
        
        this._videoPlayer = document.getElementById('video-player');
        
        this._overlayAlbumArt = document.getElementById('playback-overlay-cover');
        this._fullScreenAlbumArt = document.getElementById('album-art');
        this._episodeTitle = document.getElementById('playback-overlay-title');
        this._episodeDescription = document.getElementById('playback-overlay-description');
        this._playPosition = document.getElementById('play-position');
        this._playDuration = document.getElementById('play-duration');
        this._playbackProgress = document.getElementById('playback-progress');
        this._playerContainer = document.getElementById('player-container');
        this._idleScreen = document.getElementById('idle-screen');
        this._playbackOverlay = document.getElementById('playback-overlay');

        //Set error Handling for images
        this._overlayAlbumArt.addEventListener("load", this.onOverlayImageLoaded.bind(this));
        this._overlayAlbumArt.addEventListener("error", this.onOverlayImageLoadError.bind(this));

        this._fullScreenAlbumArt.addEventListener("load", this.onaudioAlbumArtLoaded.bind(this));
        this._fullScreenAlbumArt.addEventListener("error", this.onaudioAlbumArtLoadError.bind(this));

        this._Screensaver = new Screensaver();
        this._Screensaver.init();
    },

    onOverlayImageLoaded : function() {
        this._overlayAlbumArt.style.display = 'block';
    },

    onOverlayImageLoadError : function() {
        this._overlayAlbumArt.style.display = 'block';
        this._overlayAlbumArt.src = "unknown.png"
    },
    
    onaudioAlbumArtLoaded: function () {
        this._fullScreenAlbumArt.style.display = 'block';
    },

    onaudioAlbumArtLoadError: function () {
        this._fullScreenAlbumArt.style.display = 'block';
        this._fullScreenAlbumArt.src = "unknown.png"
    },

    getPlayer: function() {

        return this._videoPlayer;
    },
    
    loadEpisodeMetadata: function (metadata) {

        this._episodeMetadata = metadata;
        this.loadAlbumArt();
        this._Screensaver.loadEpisodeMetadata(metadata);
    },

    // Methods for updating the UI
    loadAlbumArt: function() {

        if (this._overlayAlbumArt.src == this._episodeMetadata.mediaImageUrl)
            return;

        if (this._episodeMetadata.mediaImageUrl != null && this._episodeMetadata.mediaImageUrl.length > 10) {

            this._overlayAlbumArt.style.display = 'none';
            this._overlayAlbumArt.src = this._episodeMetadata.mediaImageUrl;

            this._fullScreenAlbumArt.style.display = 'none';
            this._fullScreenAlbumArt.src = this._episodeMetadata.mediaImageUrl;

        } else {
            this._overlayAlbumArt.style.display = 'block';
            this._overlayAlbumArt.src = "unknown.png";

            this._fullScreenAlbumArt.src = "unknown.png";
        }
    },
    
    updateUI: function(sRem, sPos, progress, showIdle, isAudio, showTrackInfo, errorInfo) {

        this._playPosition.innerText = sPos;
        this._playDuration.innerText =  sRem == '' ? '' : "-" + sRem;
        this._playbackProgress.style.width = progress + "%";

        if (this._episodeMetadata) {
            this._episodeTitle.innerText = this._episodeMetadata.mediaTitle;
            this._episodeDescription.innerText = this._episodeMetadata.mediaSubtitle;
        }

        if (errorInfo) {
            this._episodeDescription.innerText = errorInfo;
        }

        // Idle vs. playback
        this.toggleIdle(showIdle)

        // Audio vs. video
        this.toggleAudioVideo(isAudio, showTrackInfo);

        // Refresh the screensaver        
        this._Screensaver.updateUI(this._videoPlayer.currentTime, this._videoPlayer.duration);
    },
        
    toggleIdle: function(show) {

        if (show) {
            this._playerContainer.style.display = 'none';
            this._idleScreen.style.display = 'block';
        } else {
            this._playerContainer.style.display = 'block';
            this._idleScreen.style.display = 'none';
        }
    },
    
    toggleAudioVideo: function(isAudio, showTrack) {
        
        if (showTrack) {
            this._playbackOverlay.style.display = 'block';
        } else {
            this._playbackOverlay.style.display = 'none';
        }

        if (isAudio) {
            this._fullScreenAlbumArt.style.display = 'block';
            this._videoPlayer.style.display = 'none';
        } else {
            this._videoPlayer.style.display = 'block';
            this._fullScreenAlbumArt.style.display = 'none';
        }
    },

    togglePPEBIndicator: function (indicator) {

        var buff = document.getElementById('buffering-container');
        var pause = document.getElementById('playing-pause-overlay');
        var error = document.getElementById('playing-error-overlay');

        buff.style.display = "none";
        pause.className = 'fade-out';
        error.style.display = "none";

        switch (indicator) {
        case 1:
            buff.style.display = "block";
            break;
        case 2:
            pause.className = 'fade-in';
            break;
        case 3:
            error.style.display = "block";
            break;
        default:
            break;
        }

    },

    toggleScreensaver: function(turnOn) {
        
        var buff = document.getElementById('screensaver');

        if (turnOn) {
            buff.style.display = "block";
            this._Screensaver.turnOn();
        } else {
            buff.style.display = "none";
            this._Screensaver.turnOff();
        }
    },
};

//============== Global methods ===============
var PAUSE_IDLE_DELAY = 8 * 60 * 1000;
var APP_CLOSE_DELAY = 5 * 60 * 1000;
var PLAYBACK_COMPLETE_IDLE_DELAY = 5 * 1000;
var SCREENSAVER_DELAY = 1 * 60 * 1000;

var env = new CCEnvironment();
var bpui = new BPUI();
var _bpApp = new BPApplication(env, bpui);

window.addEventListener('load', function () {
    _bpApp.init();
});