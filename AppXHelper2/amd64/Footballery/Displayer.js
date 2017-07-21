var Displayer = function (canvas, ctx) {
	assert(ctx !== undefined, "context is undefined");

	// These are the private functions of the Displayer
	var clearScreen = function() {
		ctx.clearRect(0, 0, canvas.width, canvas.height);
	};

	var myVisuals = [];

	var oldtime = new Date();
	var draw = function() {
		assert(myVisuals !== undefined, "myVisuals is undefined");

		clearScreen();
		for(var visual in myVisuals) {
			var drawPrimitives = myVisuals[visual].draw();
			ctx.save();
			for(var shape in drawPrimitives) {
				drawPrimitives[shape].display(ctx);
			}
			ctx.restore();
		}


		var now = new Date();
		var delta = now - oldtime;
		oldtime = now;

		ctx.font = "10px Times New Roman";
		ctx.fillStyle = "Black";
		ctx.fillText("FPS: " + Math.floor(1000/delta), 5, 30);
	};

	// Call the draw function every 16 ms
	setInterval(draw, 16);


	// We return a new object with some public functions
	return {
		addVisual : function(visual) {
			assert(visual.draw !== undefined);
			myVisuals.push(visual);
		},
		refreshScreen : function() {
			draw();
		}
	};
};

// "Namespace" Shapes
var Shapes = function() {
	// helper functions for all shapes (private to the namespace)
	var setStyle = function(ctx, color, fill) {
		var style;
		if(color.getStyle) {
			style = color.getStyle(ctx);
		}
		else {
			style = color;
		}

		if(fill) {
			ctx.fillStyle = style;
		}
		else {
			ctx.strokeStyle = style;
		}
	};
	var flush =  function(ctx, fill) {
		if(fill){
			ctx.fill();
		}
		else {
			ctx.stroke();
		}
	};

	// public objects of Shapes
	return {
		Circle: function(x, y, radius, color, fill) {
			// fill is true by default
			if(fill === undefined) {
				fill = true;
			}

			return {
				display: function(ctx) {
					assert(ctx !== undefined, "Context is undefined");

					setStyle(ctx, color, fill);

					ctx.beginPath();
					ctx.arc(x,y,radius,0,Math.PI*2);

					flush(ctx, fill);
				}
			};
		},
		Rect: function(x, y, width, height, color, fill) {
			// fill is true by default
			if(fill === undefined) {
				fill = true;
			}

			return {
				display: function(ctx) {
					assert(ctx !== undefined, "Context is undefined");

					setStyle(ctx, color, fill);

					ctx.beginPath();
					ctx.moveTo(x,y);
					ctx.lineTo(x+width, y);
					ctx.lineTo(x+width, y+height);
					ctx.lineTo(x, y+height);
					ctx.lineTo(x, y);

					flush(ctx, fill);
				}
			};
		},
		Square: function(x, y, sideLength, color, fill) {
			return new Rect(x, y, sideLength, sideLength, color, fill);
		},
		Shadow: function(x,y,blur,color) {
			if(x===undefined) {
				x = 5;
			}
			if(y===undefined) {
				y = 5;
			}
			if(blur===undefined) {
				blur = 4;
			}
			if(color===undefined) {
				color = 'rgba(0,0,0,0.25)';
			}

			return {
				display: function(ctx) {
					ctx.shadowOffsetX = x;
					ctx.shadowOffsetY = y;
					ctx.shadowBlur = blur;
					ctx.shadowColor = color;
				}
			};
		},
		Text: function(x,y,text,color,font) {
			if(font === undefined){
				font = "20px Times New Roman";
			}
			if(color === undefined){
				color = 'Black';
			}

			return {
				display: function(ctx) {
					assert(ctx !== undefined, "Context is undefined");

					ctx.textAlign = "center";
					ctx.font = font;
					ctx.fillStyle = color;
					ctx.fillText(text, x, y);
				}
			};
		}
	};
}();

// Styles "namespace"
var Styles = function(){
	return {
		RadialGradient: function(x1, y1, r1, x2, y2, r2){
			var colorStops = [];
			return {
				getStyle: function(ctx) {
					var gradient = ctx.createRadialGradient(x1,y1,r1,x2,y2,r2);
					for(var stop in colorStops) {
						gradient.addColorStop(colorStops[stop].pos, colorStops[stop].color);
					}
					return gradient;
				},
				addColorStop: function(pos,color){
					colorStops.push({pos: pos, color: color});
				}
			};
		}
	};
}();