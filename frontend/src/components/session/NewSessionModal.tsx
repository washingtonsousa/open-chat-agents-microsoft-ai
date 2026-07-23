"use client";

import { X } from "lucide-react";
import { useState } from "react";
import type { Agent, Session } from "@/types";
import { sessionApi } from "@/services/api";

interface Props {
  agents: Agent[];
  onCreated: (session: Session) => void;
  onClose: () => void;
}

export function NewSessionModal({ agents, onCreated, onClose }: Props) {
  const [title, setTitle] = useState("Nova conversa");
  const [selectedAgentId, setSelectedAgentId] = useState<string>(agents[0]?.id ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const session = await sessionApi.create(title, selectedAgentId || null);
      onCreated(session);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao criar conversa.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4 p-6">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-lg font-semibold text-gray-900">Nova conversa</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 transition-colors">
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Título</label>
            <input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Agente</label>
            {agents.length === 0 ? (
              <p className="text-sm text-amber-600">
                Nenhum agente criado. Crie um agente primeiro.
              </p>
            ) : (
              <select
                value={selectedAgentId}
                onChange={(e) => setSelectedAgentId(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {agents.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.name} — {a.llm_model}
                  </option>
                ))}
              </select>
            )}
          </div>

          {error && <p className="text-sm text-red-500">{error}</p>}

          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-lg text-sm text-gray-600 hover:bg-gray-100 transition-colors"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={submitting || agents.length === 0}
              className="px-4 py-2 rounded-lg text-sm bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              {submitting ? "Criando..." : "Iniciar conversa"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
