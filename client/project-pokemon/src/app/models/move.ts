export interface BattleMove {
    name: string;
    description: string;
    power: number | null;
    accuracy: number | null;
    moveClass: string;
    pp: number;
    currentPp: number;
    type: string;
}

export interface Movement {
    id: number;
    name: string;
    description: string;
    power: number | null;
    accuracy: number | null;
    movementClass: string;
    pp: number;
    type: string;
}
