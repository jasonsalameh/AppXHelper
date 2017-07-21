// Visuals namespace
var Visuals = {
	Goal: function(endCoord1, endCoord2, goallineCoord, goalType, canvasWidth, canvasHeight){
		assert (endCoord1 !== undefined, "endCoord1 is undefined!");
		assert (endCoord1 !== null, "endCoord1 is null!");
		assert (endCoord2 !== undefined, "endCoord2 is undefined!");
		assert (endCoord2 !== null, "endCoord2 is null!");
		assert (endCoord1 !== endCoord2, "endCoord1 is equal to endCoord2");
		assert (goallineCoord !== undefined, "goallineCoord is undefind!");
		assert (goallineCoord !== null, "goallineCoord is null!");
		assert(goalType !== undefined, "goalType is undefined!");
		assert(goalType === "Top" || goalType ==="Bottom" || goalType === "Left" || goalType === "Right", "goalType is not of valid value!");
		assert (canvasWidth !== undefined, "canvasWidth is undefined!");
		assert (canvasHeight !== undefined, "canvasHeight is undefined!");
			
		// Private members of goal
		var myEndCoords = {lesserEndCoord: 0, greaterEndCoord: 0};
		if (endCoord1 < endCoord2){
			myEndCoords.lesserEndCoord = endCoord1;
			myEndCoords.greaterEndCoord = endCoord2;
			}
		else{
			myEndCoords.lesserEndCoord = endCoord2;
			myEndCoords.greaterEndCoord = endCoord1;
			}
		var myGoallineCoord = goallineCoord;
		var myGoalType = goalType;
		var myCanvasWidth = canvasWidth;
		var myCanvasHeight = canvasHeight;
		var myRenderedSizeOfGoal = null;

		return{
			isGoal: function(point){
				assert (point !== undefined, "point is undefined!");
				assert (point.x !== undefined, "point.x is undefined!");
				assert (point.y !== undefined, "point.y is undefined!");
				// Make sure it has been set
				assert (null !== myRenderedSizeOfGoal);

				if ("Top" === myGoalType || "Bottom" === myGoalType){
					if (point.x < myEndCoords.lesserEndCoord || point.x > myEndCoords.greaterEndCoord){
						return false;
						}

					if ("Top" === myGoalType){
						if (point.y <= myGoallineCoord + myRenderedSizeOfGoal){
							return true;
							}
						else{
							return false;
							}
						}
					else if ("Bottom" === myGoalType){
						if (point.y >= myGoallineCoord - myRenderedSizeOfGoal){
							return true;
							}
						else{
							return false;
							}
						}
					}
				else if ("Left" === myGoalType || "Right" === myGoalType){
					if (point.y < myEndCoords.lesserEndCoord || point.y > myEndCoords.greaterEndCoord){
						return false;
						}
					else if ("Left" === myGoalType){
						if (point.x <= myGoallineCoord + myRenderedSizeOfGoal){
							return true;
							}
						else{
							return false;
							}
						}
					else if ("Right" === myGoalType){
						if (point.x >= myGoallineCoord - myRenderedSizeOfGoal){
							return true;
							}
						else{
							return false;
							}
						}
					}
				return false;
				},
			
			draw: function() {
				var rectangleColor = 'rgba(0,0,0,0.25)';
				var percentage = 0.01;

				var xSize;
				var ySize;
				var xCoord;
				var yCoord;

				if ("Top" === myGoalType){
					xSize = myEndCoords.greaterEndCoord - myEndCoords.lesserEndCoord;
					ySize = myCanvasHeight * percentage;
					xCoord = myEndCoords.lesserEndCoord;
					yCoord = myGoallineCoord;

					myRenderedSizeOfGoal = ySize;
				}
				else if ("Bottom" === myGoalType){
					xSize = myEndCoords.greaterEndCoord - myEndCoords.lesserEndCoord;
					ySize = myCanvasHeight * percentage;
					xCoord = myEndCoords.lesserEndCoord;
					yCoord = myGoallineCoord - ySize;

					myRenderedSizeOfGoal = ySize;
				}
				else if ("Left" === myGoalType){
					xSize = myCanvasWidth * percentage;
					ySize = myEndCoords.greaterEndCoord - myEndCoords.lesserEndCoord;;
					xCoord = myGoallineCoord;
					yCoord = myEndCoords.lesserEndCoord;

					myRenderedSizeOfGoal = xSize;
				}
				else if ("Right" === myGoalType){
					xSize = myCanvasWidth * percentage;
					ySize = myEndCoords.greaterEndCoord - myEndCoords.lesserEndCoord;;
					xCoord = myGoallineCoord - xSize;
					yCoord = myEndCoords.lesserEndCoord;

					myRenderedSizeOfGoal = xSize;
				}

				assert(xCoord !== undefined, "xCoord is undefined");
				assert(yCoord !== undefined, "yCoord is undefined");
				assert(xSize !== undefined, "xSize is undefined");
				assert(ySize !== undefined, "ySize is undefined");

				return [
					new Shapes.Rect(xCoord, yCoord, xSize, ySize, rectangleColor),
					new Shapes.Rect(xCoord, yCoord, xSize, ySize, 'black', false)
				];
				}
			};
		},
	Ball: function(){
		// private members of Ball
		var mySpeed = { x:0, y:0 };
		var myPosition = { x: canvasWidth/2, y: canvasHeight/2 };
		var myRadius = 25;
		var myMaxSpeed = 1000;
		var mySuspendCalculation = false;

		var sendBallRedrawEvent = function(newPosition) {
			updater.processBallUpdate(newPosition);
		}

		var calculateNewPosition = function(frequencyInMs) {
			if (mySuspendCalculation){
				return;
			}
			// Update position
			var newPosition = {
				x: myPosition.x + mySpeed.x * ( frequencyInMs / 1000 ),
				y: myPosition.y + mySpeed.y * ( frequencyInMs / 1000 )
			};

			// Update speed if necessary
			if(connectionType === "Server") {
				var newSpeed;
				if(newPosition.x - myRadius < 0 || newPosition.x + myRadius > canvasWidth) {
					newSpeed = {
						x: -mySpeed.x,
						y: mySpeed.y
					};

					if(newPosition.x - myRadius < 0) {
						newPosition.x = myRadius;
					}
					else {
						assert(newPosition.x + myRadius > canvasWidth, "newPosition.x is not out of bounds");
						newPosition.x = canvasWidth - myRadius;
					}
				}
				else if(newPosition.y - myRadius < 0 || newPosition.y + myRadius > canvasHeight) {
					newSpeed = {
						x: mySpeed.x,
						y: -mySpeed.y
					};
					if(newPosition.y - myRadius < 0) {
						newPosition.y = myRadius;
					}
					else {
						assert(newPosition.y + myRadius > canvasHeight, "newPosition.y is not out of bounds");
						newPosition.y = canvasHeight - myRadius;
					}
				}

				if(newSpeed) {
					updater.processBallSpeedUpdate(newSpeed);
				}
			}
			sendBallRedrawEvent(newPosition);
		};

		// Constantly refresh the ball's position
		var refreshFrequency = 16;
		if(connectionType==='Server') {
			setInterval(function() { return calculateNewPosition(refreshFrequency); }, refreshFrequency);
		}

		// public members of Ball
		return {
			setSpeed: function(speedVector) {
				assert(speedVector !== undefined, "speedVector is undefined");
				assert(speedVector.x !== undefined, "speedVector.x is undefined");
				assert(speedVector.y !== undefined, "speedVector.y is undefined");

				mySpeed = speedVector;			
			},
			getPosition: function() {
				return myPosition;
			},
			setPosition: function(newPosition) {
				assert(newPosition !== undefined);
				assert(newPosition.x !== undefined);
				assert(newPosition.y !== undefined);

				myPosition = newPosition;
			},
			getRadius: function(){
				return myRadius;
			},
			draw: function() {
				var style = new Styles.RadialGradient(myPosition.x-myRadius/4, myPosition.y-myRadius/4, 0, myPosition.x, myPosition.y, myRadius);
				style.addColorStop(0.3, '#e84d20');
				style.addColorStop(1, '#912c0f');
				return [
					// Commenting out since this is too perf intensive on fullscreen mode
					//new Shapes.Shadow(-20*mySpeed.x/myMaxSpeed, -20*mySpeed.y/myMaxSpeed),
					new Shapes.Circle(myPosition.x, myPosition.y, myRadius, style)
				];
			},
			suspendCalculation: function(){
				mySuspendCalculation = true;
			},
			unsuspendCalculation: function(){
				mySuspendCalculation = false;
			},
			getSpeedFromTouch: function(point) {
				var touchRadius = 2*myRadius;
				var distanceToCenter = Math.sqrt(Math.pow(point.x - myPosition.x, 2) + Math.pow(point.y - myPosition.y,2));
				
				// If point is outside of the ball, we return undefined, indicating that this event doesn't affect
				// the ball's speed
				if(distanceToCenter > touchRadius) {
					return;
				}

				// return the new speed, which depends on the distance from the center of the ball
				// and it is normalized to be independent of the radius
				return {
					x: myMaxSpeed*(myPosition.x - point.x)/touchRadius,
					y: myMaxSpeed*(myPosition.y - point.y)/touchRadius
				};
			}
		};
	},
	Field: function() {
		return {
			draw: function() {
				var gradient1 = new Styles.RadialGradient(canvasWidth/5, canvasHeight/4, 0, canvasWidth/5, canvasHeight/4, canvasWidth/2);
				gradient1.addColorStop(0,'#b3ff80');
				gradient1.addColorStop(0.75, '#76ca3e');

				var gradient2 = new Styles.RadialGradient(4*canvasWidth/5, 3*canvasHeight/4, 0, 4*canvasWidth/5, 3*canvasHeight/4, canvasWidth/2);
				gradient2.addColorStop(0,'#2b4d15');
				gradient2.addColorStop(0.75, '#40721f');
				return [
					new Shapes.Rect(0, 0, canvasWidth, canvasHeight, gradient1),
					new Shapes.Rect(canvasWidth/2, 0, canvasWidth/2, canvasHeight, gradient2),
					new Shapes.Rect(canvasWidth/2-1, 0, 2, canvasHeight, '#114411'),
					new Shapes.Rect(0, 0, canvasWidth, canvasHeight, '#114411', false),
					new Shapes.Circle(canvasWidth/2, canvasHeight/2, canvasWidth/10, '#114411', false)
				];
			}
		};
	},
	Scoreboard: function(xCoord, yCoord){
		// Private members of Scoreboard
		var myScore = 0;
		var myDisplayPoint = {x: xCoord, y: yCoord};

		return {
			reset: function(){
				myScore = 0;
			},
			getScore: function(){
				return myScore;
			},
			incrementScore: function(){
				myScore++;
			},
			draw: function(){
				return [new Shapes.Text(myDisplayPoint.x, myDisplayPoint.y, myScore, "rgba(0,0,0,0.25)", "150px Segoe UI")];
			}
		};
	},
	WinningMessage: function(){
		var myMessage = "";
		return{
			updateMessage : function(message){
				assert(undefined !== message, "message is undefined!");
				myMessage = message;
			},
			draw: function(){
				return [
					new Shapes.Shadow(),
					new Shapes.Text(canvasWidth/2, (canvasHeight/2) + (canvasHeight * 0.3), myMessage, "rgba(255,255,255,0.75)", "100px Segoe UI")
				];
			}
		}
	}
};