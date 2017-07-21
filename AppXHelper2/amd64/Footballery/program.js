var assert = function (condition, message)
{
    if(!condition) {
        alert(message);
		throw message;
    }
};

// "Server" or "Client"

var connectionType;
var ip;
var displayer;
var ball;
var eventProcessor;
var updater;
var goal1;
var goal2;
var scoreboard1;
var scoreboard2;
var winningMessage;

var canvasHeight = 500;
var canvasWidth = 600;

function getUrlParams() {
	var vars = {};
	var hashes = window.location.href.slice(window.location.href.indexOf('?')+1).split('&');
	for(var i = 0; i < hashes.length; i++) {
		var hash = hashes[i].split('=');
		vars[hash[0]] = hash[1];
	}

	return vars;
}

function initialize() {
	// initialize canvas
    var canvas = document.getElementById('FootballeryCanvas');
    assert(canvas !== undefined, "there is no canvas element in the document");

	canvas.width = document.body.clientWidth;
	canvas.height = document.body.clientHeight;

	canvasWidth = canvas.width;
	canvasHeight = canvas.height;

    var ctx = canvas.getContext('2d');
    assert(ctx !== undefined, "context is undefined");

	// Check for URL GET parameters
	var urlParams = getUrlParams();
	assert(urlParams["type"] !== undefined, "There is no type url param");
	connectionType = urlParams["type"];

	if(connectionType === "Client") {
		assert(urlParams["ip"] !== undefined, "There is no ip url param when connecting as client");
		ip = urlParams["ip"];
	}

	// Connect
	connect();

	// Create displayer
	displayer = new Displayer(canvas, ctx);
	addVisuals();

	updater = new Updater();

	addHandler(canvas, 'MSTouchDown', eventProcessor);
}

var connect = function() {
	if(connectionType === "Server") {
		StartServer();
	}
	else {
		assert(connectionType === "Client", "connectionType is invalid");
		StartClient();
	}
}

var addVisuals = function() {
	assert(displayer !== undefined, "Displayer is undefined!");
	
	// Create field
	displayer.addVisual(new Visuals.Field());

	// Create ball
	ball = new Visuals.Ball();
	displayer.addVisual(ball);

	// Vertical Goals (when the screen is tilted sideways)
	var sizeOfGoal = canvasHeight * 0.20;
	// start at middle and subtract half of the size of the goal
	var yStartCoordOfGoal = (canvasHeight * 0.5) - (0.5*sizeOfGoal);
	goal1 = new Visuals.Goal(yStartCoordOfGoal, yStartCoordOfGoal + sizeOfGoal, 0, "Left", canvasWidth, canvasHeight);
	goal2 = new Visuals.Goal(yStartCoordOfGoal, yStartCoordOfGoal + sizeOfGoal, canvasWidth, "Right", canvasWidth, canvasHeight);
	displayer.addVisual(goal1);
	displayer.addVisual(goal2);

	// Create scoreboards
	var yCoord = canvasHeight /4;
	var scoreboard1XCoord = (canvasWidth / 2) - (canvasWidth * 0.25);
	scoreboard1 = new Visuals.Scoreboard(scoreboard1XCoord, yCoord);
	var scoreboard2XCoord = (canvasWidth / 2) + (canvasWidth * 0.25);
	scoreboard2 = new Visuals.Scoreboard(scoreboard2XCoord, yCoord);
	displayer.addVisual(scoreboard1);
	displayer.addVisual(scoreboard2);

	winningMessage = new Visuals.WinningMessage();
	displayer.addVisual(winningMessage);
};