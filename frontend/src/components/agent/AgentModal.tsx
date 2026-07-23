"use client";

import { X } from "lucide-react";
import type { Agent } from "@/types";
import { AgentForm } from "./AgentForm";

interface Props {
  onCreated: (agent: Agent) => void;
  onClose: () => void;
}

export function AgentModal({ onCreated, onClose }: Props) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg mx-4 p-6">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-lg font-semibold text-gray-900">Novo agente</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 transition-colors">
            <X size={20} />
          </button>
        </div>
        <AgentForm onCreated={onCreated} onCancel={onClose} />
      </div>
    </div>
  );
}
