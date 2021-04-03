﻿import * as sR from "signalR";
import Vue from "vue";
import { WorldData } from "./WorldData";

import "./BlockView";
import "./KillData";
import "./OutfitKillData";

const vm = new Vue({
	el: "#app",

	created: function(): void {
		this.socketState = "unconnected";

		const conn = new sR.HubConnectionBuilder()
			.withUrl("/ws/data")
			.withAutomaticReconnect([5000, 10000, 20000, 20000])
			.build();

		conn.on("DataUpdate", (data: any) => {
			this.worldData = JSON.parse(data);
			this.lastUpdate = new Date();
		});

		conn.start().then(() => {
			this.socketState = "opened";
		}).catch(err => {
			console.error(err);
		});

		conn.onreconnected(() => { this.socketState = "opened"; });

		conn.onclose((err?: Error) => {
			this.socketState = "closed";
			if (err) {
				console.error("onclose: ", err);
			}
		});

		conn.onreconnecting((err?: Error) => {
			this.socketState = "reconnecting";
			if (err) {
				console.error("onreconnecting: ", err);
			}
		});
	},

	data: {
		worldData: new WorldData() as WorldData,
		socketState: "" as string,
		lastUpdate: new Date() as Date
	},

	methods: {

	}

});
(window as any).vm = vm;