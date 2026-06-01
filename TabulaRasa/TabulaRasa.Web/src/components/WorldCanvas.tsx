"use client";

import { useEffect, useRef } from "react";
import * as PIXI from "pixi.js";
import type { GridCell, HoverInfo, OccupiedCell, Selection, SimulationDraft, SimulationSnapshot } from "@/types/simulation";

type Props = {
  snapshot: SimulationSnapshot | null;
  draft: SimulationDraft | null;
  editing: boolean;
  canEdit: boolean;
  selection: Selection;
  onSelect: (selection: Selection) => void;
  onMoveAgent: (id: string, cell: GridCell) => void;
  onMoveFood: (id: string, cell: GridCell) => void;
  onToggleBlockedCell: (cell: GridCell) => void;
  onHover: (hover: HoverInfo) => void;
};

const cellSize = 58;

export function WorldCanvas({
  snapshot,
  draft,
  editing,
  canEdit,
  selection,
  onSelect,
  onMoveAgent,
  onMoveFood,
  onToggleBlockedCell,
  onHover
}: Props) {
  const hostRef = useRef<HTMLDivElement | null>(null);
  const appRef = useRef<PIXI.Application | null>(null);
  const worldRef = useRef<PIXI.Container | null>(null);
  const viewRef = useRef({ x: 40, y: 40, scale: 1 });
  const dragRef = useRef<
    | {
        pointerId: number;
        x: number;
        y: number;
        viewX: number;
        viewY: number;
      }
    | null
  >(null);

  useEffect(() => {
    if (!hostRef.current) {
      return;
    }

    let disposed = false;
    const app = new PIXI.Application();
    const host = hostRef.current;

    app
      .init({
        background: "#101114",
        antialias: true,
        resizeTo: host
      })
      .then(() => {
        if (disposed) {
          app.destroy();
          return;
        }

        host.appendChild(app.canvas);
        appRef.current = app;
        worldRef.current = new PIXI.Container();
        app.stage.addChild(worldRef.current);
        renderWorld();
      });

    const handleWheel = (event: WheelEvent) => {
      event.preventDefault();
      const view = viewRef.current;
      const nextScale = Math.min(2.2, Math.max(0.45, view.scale + (event.deltaY > 0 ? -0.08 : 0.08)));
      view.scale = nextScale;
      renderWorld();
    };

    const handlePointerDown = (event: PointerEvent) => {
      if (event.button !== 0) {
        return;
      }

      event.preventDefault();
      dragRef.current = {
        pointerId: event.pointerId,
        x: event.clientX,
        y: event.clientY,
        viewX: viewRef.current.x,
        viewY: viewRef.current.y
      };
      host.classList.add("is-panning");
    };

    const handlePointerMove = (event: PointerEvent) => {
      const drag = dragRef.current;
      if (!drag || drag.pointerId !== event.pointerId) {
        return;
      }

      event.preventDefault();
      viewRef.current.x = drag.viewX + event.clientX - drag.x;
      viewRef.current.y = drag.viewY + event.clientY - drag.y;
      renderWorld();
    };

    const handlePointerUp = (event: PointerEvent) => {
      if (dragRef.current?.pointerId === event.pointerId) {
        dragRef.current = null;
        host.classList.remove("is-panning");
      }
    };

    host.addEventListener("wheel", handleWheel, { capture: true, passive: false });
    host.addEventListener("pointerdown", handlePointerDown, true);
    window.addEventListener("pointermove", handlePointerMove, { capture: true, passive: false });
    window.addEventListener("pointerup", handlePointerUp, true);
    window.addEventListener("pointercancel", handlePointerUp, true);

    return () => {
      disposed = true;
      host.removeEventListener("wheel", handleWheel, true);
      host.removeEventListener("pointerdown", handlePointerDown, true);
      window.removeEventListener("pointermove", handlePointerMove, true);
      window.removeEventListener("pointerup", handlePointerUp, true);
      window.removeEventListener("pointercancel", handlePointerUp, true);
      app.destroy(true);
      appRef.current = null;
      worldRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    renderWorld();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [snapshot, draft, editing, canEdit, selection]);

  function renderWorld() {
    const world = worldRef.current;
    if (!world || !snapshot) {
      return;
    }

    world.removeChildren();
    world.x = viewRef.current.x;
    world.y = viewRef.current.y;
    world.scale.set(viewRef.current.scale);

    const blocked = new Set(snapshot.grid.blockedCells.map((cell) => `${cell.x}:${cell.y}`));
    const draftBlocked = new Set(draft?.grid.blockedCells.map((cell) => `${cell.x}:${cell.y}`));
    const occupiedCells = editing && draft ? draftOccupiedCells(draft) : snapshot.grid.occupiedCells;
    const occupiedByCell = new Map<string, OccupiedCell[]>();

    for (const occupied of occupiedCells) {
      const key = `${occupied.cell.x}:${occupied.cell.y}`;
      occupiedByCell.set(key, [...(occupiedByCell.get(key) ?? []), occupied]);
    }

    for (let y = 0; y < snapshot.grid.height; y++) {
      for (let x = 0; x < snapshot.grid.width; x++) {
        const isBlocked = editing && draft ? draftBlocked.has(`${x}:${y}`) : blocked.has(`${x}:${y}`);
        const occupants = occupiedByCell.get(`${x}:${y}`) ?? [];
        const cell = new PIXI.Graphics();
        cell.rect(x * cellSize, y * cellSize, cellSize - 1, cellSize - 1);
        cell.fill(isBlocked ? 0x17191d : occupants.length > 0 ? 0x24394a : (x + y) % 2 === 0 ? 0x20262b : 0x243225);
        cell.stroke({ color: occupants.length > 0 ? 0x6aa8ff : 0x3a414d, width: occupants.length > 0 ? 2 : 1 });
        cell.eventMode = "static";
        cell.cursor = editing && canEdit ? "pointer" : "default";
        cell.on("pointertap", () => {
          const selectedCell = { x, y };
          if (editing && canEdit && selection?.type === "cell") {
            onToggleBlockedCell(selectedCell);
            return;
          }
          onSelect({ type: "cell", cell: selectedCell });
        });
        cell.on("pointerover", (event) =>
          onHover({
            label: `Cell ${x}, ${y}`,
            detail: cellDetail(isBlocked, occupants),
            x: event.global.x,
            y: event.global.y
          })
        );
        cell.on("pointermove", (event) =>
          onHover({
            label: `Cell ${x}, ${y}`,
            detail: cellDetail(isBlocked, occupants),
            x: event.global.x,
            y: event.global.y
          })
        );
        cell.on("pointerout", () => onHover(null));
        world.addChild(cell);
      }
    }

    for (const movement of snapshot.activeMovements) {
      if (movement.waypoints.length === 0) {
        continue;
      }

      const route = new PIXI.Graphics();
      route.moveTo(
        movement.waypoints[0].x * cellSize + cellSize / 2,
        movement.waypoints[0].y * cellSize + cellSize / 2
      );

      for (const waypoint of movement.waypoints.slice(1)) {
        route.lineTo(waypoint.x * cellSize + cellSize / 2, waypoint.y * cellSize + cellSize / 2);
      }

      route.stroke({ color: 0x6aa8ff, width: 3, alpha: 0.75 });
      world.addChild(route);
    }

    const foodItems = editing && draft ? draft.food : snapshot.food;
    for (const food of foodItems) {
      const graphic = new PIXI.Graphics();
      const x = food.position.x * cellSize + cellSize / 2;
      const y = food.position.y * cellSize + cellSize / 2;
      const selected = selection?.type === "food" && selection.id === food.id;
      graphic.circle(x, y, selected ? 16 : 12);
      graphic.fill(food.isConsumed ? 0x5a5f68 : 0xf4c95d);
      graphic.stroke({ color: selected ? 0xffffff : 0x7b5525, width: selected ? 4 : 2 });
      graphic.eventMode = "static";
      graphic.cursor = editing && canEdit ? "grab" : "pointer";
        graphic.on("pointertap", () => onSelect({ type: "food", id: food.id }));
      graphic.on("pointerover", (event) =>
        onHover({
          label: food.id,
          detail: food.isConsumed ? "Food - consumed" : "Food - available",
          x: event.global.x,
          y: event.global.y
        })
      );
      graphic.on("pointermove", (event) =>
        onHover({
          label: food.id,
          detail: food.isConsumed ? "Food - consumed" : "Food - available",
          x: event.global.x,
          y: event.global.y
        })
      );
      graphic.on("pointerout", () => onHover(null));
      graphic.on("rightclick", () => {
        if (editing && canEdit) {
          onMoveFood(food.id, toCell(food.position.x, food.position.y));
        }
      });
      world.addChild(graphic);
    }

    const agents = editing && draft ? draft.agents : snapshot.agents;
    for (const agent of agents) {
      const graphic = new PIXI.Graphics();
      const x = agent.position.x * cellSize + cellSize / 2;
      const y = agent.position.y * cellSize + cellSize / 2;
      const selected = selection?.type === "agent" && selection.id === agent.id;
      graphic.roundRect(x - 17, y - 17, 34, 34, 6);
      graphic.fill(0x54c475);
      graphic.stroke({ color: selected ? 0xffffff : 0x153b24, width: selected ? 4 : 2 });
      graphic.eventMode = "static";
      graphic.cursor = editing && canEdit ? "grab" : "pointer";
      graphic.on("pointertap", () => onSelect({ type: "agent", id: agent.id }));
      graphic.on("pointerover", (event) =>
        onHover({
          label: agent.id,
          detail: `Agent - hunger ${formatNumber(agent.needs.hunger)}`,
          x: event.global.x,
          y: event.global.y
        })
      );
      graphic.on("pointermove", (event) =>
        onHover({
          label: agent.id,
          detail: `Agent - hunger ${formatNumber(agent.needs.hunger)}`,
          x: event.global.x,
          y: event.global.y
        })
      );
      graphic.on("pointerout", () => onHover(null));
      world.addChild(graphic);
    }
  }

  function toCell(x: number, y: number): GridCell {
    return { x: Math.floor(x), y: Math.floor(y) };
  }

  useEffect(() => {
    if (!editing || !canEdit || !hostRef.current) {
      return;
    }

    const host = hostRef.current;
    const handleDoubleClick = (event: MouseEvent) => {
      if (!selection || selection.type === "cell") {
        return;
      }

      const rect = host.getBoundingClientRect();
      const view = viewRef.current;
      const worldX = (event.clientX - rect.left - view.x) / view.scale;
      const worldY = (event.clientY - rect.top - view.y) / view.scale;
      const cell = {
        x: Math.max(0, Math.floor(worldX / cellSize)),
        y: Math.max(0, Math.floor(worldY / cellSize))
      };

      if (selection.type === "agent") {
        onMoveAgent(selection.id, cell);
      } else {
        onMoveFood(selection.id, cell);
      }
    };

    host.addEventListener("dblclick", handleDoubleClick);

    return () => host.removeEventListener("dblclick", handleDoubleClick);
  }, [canEdit, editing, onMoveAgent, onMoveFood, selection]);

  return <div className="canvas-host" ref={hostRef} />;
}

function formatNumber(value: number) {
  return Number.isInteger(value) ? value.toString() : value.toFixed(1);
}

function draftOccupiedCells(draft: SimulationDraft): OccupiedCell[] {
  return [
    ...draft.agents.map((agent) => ({
      cell: { x: Math.floor(agent.position.x), y: Math.floor(agent.position.y) },
      entityId: agent.id,
      entityType: "AgentEntity"
    })),
    ...draft.food
      .filter((food) => !food.isConsumed)
      .map((food) => ({
        cell: { x: Math.floor(food.position.x), y: Math.floor(food.position.y) },
        entityId: food.id,
        entityType: "FoodEntity"
      }))
  ];
}

function cellDetail(isBlocked: boolean, occupants: OccupiedCell[]) {
  const state = isBlocked ? "blocked" : "open";
  const occupied = occupants.length === 0
    ? "unoccupied"
    : `occupied by ${occupants.map((occupant) => occupant.entityId).join(", ")}`;

  return `${state} - ${occupied}`;
}
