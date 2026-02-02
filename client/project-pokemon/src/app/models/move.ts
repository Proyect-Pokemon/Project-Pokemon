import { Signal } from "@angular/core";

export interface Move {
    name: string;
    description: string;
    power?: number;
    //accuracy: number;
    moveClass: string;
    pp: number;
    currentPP: Signal<number>;
    type: string;
}
