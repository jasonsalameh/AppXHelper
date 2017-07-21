var Updater = function() {
	var myLastKnownBallPosition = {x: canvasWidth/2, y: canvasHeight/2};

	var calculateGameConditions = function(ballPosition){
		assert(undefined !== ballPosition, "ballPosition is undefined!");
		assert(undefined !== ballPosition.x, "ballPosition.x is undefined!");
		assert(undefined !== ballPosition.y, "ballPosition.y is undefined!");

		if (myLastKnownBallPosition.x !== ballPosition.x || myLastKnownBallPosition.y !== ballPosition.y){
			winningMessage.updateMessage("");
		}

		myLastKnownBallPosition.x = ballPosition.x;
		myLastKnownBallPosition.y = ballPosition.y;

		ball.setPosition(ballPosition);

		// Calculate whether a goal happened or not
		if(connectionType === "Server") {
			var radiusOfBall = ball.getRadius();
			// If one of the four points of the circle are in a goal, then it is a goal
			var topPoint = {x: ballPosition.x, y: ballPosition.y - radiusOfBall};
			var bottomPoint = {x: ballPosition.x, y: ballPosition.y + radiusOfBall};
			var leftPoint = {x: ballPosition.x - radiusOfBall, y: ballPosition.y};
			var rightPoint = {x: ballPosition.x + radiusOfBall, y: ballPosition.y};

			var resetBallPosition = {x: canvasWidth/2, y: canvasHeight/2};
			var resetBallSpeed = {x: 0, y: 0};
			
			var winningScore = 3;
			var isGoalForGoal1 = goal1.isGoal(topPoint) || goal1.isGoal(bottomPoint) || goal1.isGoal(leftPoint) || goal1.isGoal(rightPoint);
			var isGoalForGoal2 = goal2.isGoal(topPoint) || goal2.isGoal(bottomPoint) || goal2.isGoal(leftPoint) || goal2.isGoal(rightPoint);
			if (true === isGoalForGoal1 || true === isGoalForGoal2){
				ball.suspendCalculation();
				updater.processBallUpdate(resetBallPosition);
				updater.processBallSpeedUpdate(resetBallSpeed);
				ball.unsuspendCalculation();
				myLastKnownBallPosition.x = resetBallPosition.x;
				myLastKnownBallPosition.y = resetBallPosition.y;
				//displayer.refreshScreen();
			}

			if (true === isGoalForGoal1){
				scoreboard2.incrementScore();
				//displayer.refreshScreen();
				var score = scoreboard2.getScore();
				if (score >= winningScore){
					winningMessage.updateMessage("Player 2 wins!");
					scoreboard1.reset();
					scoreboard2.reset();
				}
			}
			else if (true === isGoalForGoal2){
				scoreboard1.incrementScore();
				//displayer.refreshScreen();
				var score = scoreboard1.getScore();
				if (score >= winningScore){
					winningMessage.updateMessage("Player 1 wins!");
					scoreboard1.reset();
					scoreboard2.reset();
				}
			}
		}
	};

	return {
		processTouchUpdate: function(point) {
			assert(point !== undefined);
			assert(point.x !== undefined);
			assert(point.y !== undefined);

			var speed = ball.getSpeedFromTouch(point);
			if(speed === undefined) {
				return;
			}

			this.processBallSpeedUpdate(speed);
		},
		processNetworkUpdate: function() {
			// TODO
		},
		processBallUpdate: function(ballPosition) {
			assert(ballPosition !== undefined);
			assert(ballPosition.x !== undefined)
			assert(ballPosition.y !== undefined)

			// BUG: 181505
			/*
			var data = new Windows.Networking.Data();
			data.WriteString("BallPositionUpdate");
			data.WriteDouble(ballPosition.x);
			data.WriteDouble(ballPosition.y);
			WriteOperation(socket, ip, data);
			*/

			calculateGameConditions(ballPosition);
		},
		processBallSpeedUpdate: function(speed) {
			assert(speed !== undefined, "speed is undefined");
			assert(speed.x !== undefined, "speed.x is undefined");
			assert(speed.y !== undefined, "speed.y is undefined");
			
			// BUG:  181505
			/*
			var data = new Windows.Networking.Data();
			data.WriteString("BallSpeedUpdate");
			data.WriteDouble(speed.x);
			data.WriteDouble(speeed.y);
			WriteOperation(socket, ip, data);
			*/

			ball.setSpeed(speed);
		}
	};
};