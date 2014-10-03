

function Screensaver() {
}

Screensaver.prototype = {
    _Container: null,
    _episodeMetadata: null,
    _Title: null,
    _Description: null,
    _Duration: null,
    _ScreenWidth: -1,
    _ScreenHeight: -1,
    _RepeatTimeoutID: null,
    _IsEnabled: false,
    _ScreenPadding: 20,
    _Clock: null,

    init: function() {

        this._Container = document.getElementById("ssaver-container");
        this._AlbumArt = document.getElementById("ssaver-art");
        this._Title = document.getElementById("ssaver-title");
        this._Description = document.getElementById("ssaver-description");
        this._Duration = document.getElementById("ssaver-remaining");
        this._Clock = document.getElementById("ssaver-clock");
        this._ScreenWidth = window.innerWidth, this._ScreenHeight = window.innerHeight;

        this.reset();
    },
    
    reset: function() {
        console.log("Screensaver reset...");
    },
    
    loadEpisodeMetadata: function (metadata) {

        this._episodeMetadata = metadata;

        if (this._episodeMetadata.mediaImageUrl != null && this._episodeMetadata.mediaImageUrl.length > 10) {
            this._AlbumArt.src = this._episodeMetadata.mediaImageUrl;
        } else {
            this._AlbumArt.src = "unknown.png";
        }

        this._Title.innerText = this._episodeMetadata.mediaTitle;
        this._Description.innerText = this._episodeMetadata.mediaSubtitle;
        this._Duration.innerText = "";
    },
    
    updateUI: function (playerTime, totalTime) {
        if (this._episodeMetadata) {
            
            this._Duration.innerText = this.formatTime(totalTime - playerTime) + " remaining";
        }
        
        var today = new Date();
        var h = today.getHours();
        var m = today.getMinutes();
        this._Clock.innerText = h + ":" + (m < 10 ? "0":"") + m;
    },
    
    formatTime : function(rawSec) {
        var sec = Math.floor(rawSec);
        var hr = Math.floor(sec / 3600);
        var min = Math.floor((sec - (hr * 3600)) / 60);
        sec -= ((hr * 3600) + (min * 60));
        sec = Math.round(sec);

        sec = (hr > 0 || min > 0) && sec < 10 ? '0' + sec : sec;
        min = (min) ? min + " min." : '';
        hr = (hr) ? (hr + " hr. ") : '';

        if (hr.length == 0 && min.length == 0)
            return sec + " sec.";
        else
            return hr + min;
    },

    drawLoop: function() {

        var maxWidth = this._ScreenWidth - 2 * this._ScreenPadding - this._Container.offsetWidth;
        var maxHeight = this._ScreenHeight - 2 * this._ScreenPadding - this._Container.offsetHeight;
        this._Container.style.left = Math.floor((Math.random() * maxWidth) + this._ScreenPadding)  + "px";
        this._Container.style.top = Math.floor((Math.random() * maxHeight) + this._ScreenPadding) + "px";
        
        var r = Math.round((Math.random() * 100) + 100);
        var g = Math.round((Math.random() * 100) + 100);
        var b = Math.round((Math.random() * 100) + 100);
        var a = 0.20;

        this._Clock.style.color = "rgba(" + r + ", " + g + ", " + b + ", " + a + ")";

        if (this._RepeatTimeoutID != null) 
            clearInterval(this._RepeatTimeoutID);
     
        this._RepeatTimeoutID = null;
        
        if (this._IsEnabled)
            this._RepeatTimeoutID = setTimeout(this.drawLoop.bind(this), 15*1000);
    },

    turnOn: function () {
        this._IsEnabled = true;
        this.drawLoop();
    },
    
    turnOff : function() {
        if (this._RepeatTimeoutID != null) 
            clearInterval(this._RepeatTimeoutID);
        
        this._RepeatTimeoutID = null;
        this._IsEnabled = false;
    }
};
	
	
