"use client";

import clsx from "clsx";
import { Bot, Plus, Trash2 } from "lucide-react";
import type { Agent, Session } from "@/types";

interface Props {
  sessions: Session[];
  agents: Agent[];
  activeSessionId: string | null;
  onSelect: (id: string) => void;
  onNewSession: () => void;
  onNewAgent: () => void;
  onDeleteSession: (id: string) => void;
  onDeleteAgent: (id: string) => void;
}

export function SessionSidebar({
  sessions,
  agents,
  activeSessionId,
  onSelect,
  onNewSession,
  onNewAgent,
  onDeleteSession,
  onDeleteAgent,
}: Props) {
  return (
    <aside className="flex flex-col w-64 h-full bg-gray-900 text-white overflow-hidden">
      <div className="p-4 border-b border-gray-700">
        <h1 className="text-lg font-semibold">open-chat-agents</h1>
      </div>

      <div className="flex-1 overflow-y-auto">
        {/* Agents section */}
        <div className="px-3 pt-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-gray-400">
              Agentes
            </span>
            <button
              onClick={onNewAgent}
              className="text-gray-400 hover:text-white transition-colors"
              title="Novo agente"
            >
              <Plus size={14} />
            </button>
          </div>

          {agents.length === 0 ? (
            <p className="text-xs text-gray-500 px-1 py-1">Nenhum agente criado.</p>
          ) : (
            <div className="space-y-0.5">
              {agents.map((a) => (
                <div
                  key={a.id}
                  className="group flex items-center justify-between rounded-lg px-2 py-1.5 text-sm hover:bg-gray-800 transition-colors"
                >
                  <div className="flex items-center gap-2 truncate">
                    <Bot size={13} className="text-blue-400 shrink-0" />
                    <div className="truncate">
                      <p className="truncate text-xs font-medium">{a.name}</p>
                      <p className="truncate text-xs text-gray-400">{a.llm_model}</p>
                    </div>
                  </div>
                  <button
                    onClick={() => onDeleteAgent(a.id)}
                    className="opacity-0 group-hover:opacity-100 ml-1 text-gray-400 hover:text-red-400 transition-opacity shrink-0"
                  >
                    <Trash2 size={13} />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Sessions section */}
        <div className="px-3 pt-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-gray-400">
              Conversas
            </span>
            <button
              onClick={onNewSession}
              className="text-gray-400 hover:text-white transition-colors"
              title="Nova conversa"
            >
              <Plus size={14} />
            </button>
          </div>

          {sessions.length === 0 ? (
            <p className="text-xs text-gray-500 px-1 py-1">Nenhuma conversa ainda.</p>
          ) : (
            <div className="space-y-0.5">
              {sessions.map((s) => (
                <div
                  key={s.id}
                  className={clsx(
                    "group flex items-center justify-between rounded-lg px-2 py-1.5 cursor-pointer transition-colors",
                    s.id === activeSessionId ? "bg-gray-700" : "hover:bg-gray-800"
                  )}
                  onClick={() => onSelect(s.id)}
                >
                  <div className="truncate flex-1">
                    <p className="truncate text-xs font-medium">{s.title}</p>
                    {s.agent && (
                      <p className="truncate text-xs text-gray-400">{s.agent.name}</p>
                    )}
                  </div>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onDeleteSession(s.id);
                    }}
                    className="opacity-0 group-hover:opacity-100 ml-1 text-gray-400 hover:text-red-400 transition-opacity shrink-0"
                  >
                    <Trash2 size={13} />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </aside>
  );
}
