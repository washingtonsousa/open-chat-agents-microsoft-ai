"use client";

import { useEffect, useState } from "react";
import { agentApi, sessionApi } from "@/services/api";
import { AgentModal } from "@/components/agent/AgentModal";
import { ChatWindow } from "@/components/chat/ChatWindow";
import { SessionSidebar } from "@/components/chat/SessionSidebar";
import { NewSessionModal } from "@/components/session/NewSessionModal";
import type { Agent, Session } from "@/types";

export default function Home() {
  const [sessions, setSessions] = useState<Session[]>([]);
  const [agents, setAgents] = useState<Agent[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [showAgentModal, setShowAgentModal] = useState(false);
  const [showSessionModal, setShowSessionModal] = useState(false);

  useEffect(() => {
    Promise.all([sessionApi.list(), agentApi.list()]).then(([s, a]) => {
      setSessions(s.sessions);
      setAgents(a.agents);
      if (s.sessions.length > 0) setActiveId(s.sessions[0].id);
    });
  }, []);

  function handleAgentCreated(agent: Agent) {
    setAgents((prev) => [agent, ...prev]);
    setShowAgentModal(false);
  }

  function handleSessionCreated(session: Session) {
    setSessions((prev) => [session, ...prev]);
    setActiveId(session.id);
    setShowSessionModal(false);
  }

  async function handleDeleteSession(id: string) {
    await sessionApi.delete(id);
    setSessions((prev) => prev.filter((s) => s.id !== id));
    if (activeId === id) {
      const remaining = sessions.filter((s) => s.id !== id);
      setActiveId(remaining.length > 0 ? remaining[0].id : null);
    }
  }

  async function handleDeleteAgent(id: string) {
    await agentApi.delete(id);
    setAgents((prev) => prev.filter((a) => a.id !== id));
  }

  return (
    <div className="flex h-full">
      <SessionSidebar
        sessions={sessions}
        agents={agents}
        activeSessionId={activeId}
        onSelect={setActiveId}
        onNewSession={() => setShowSessionModal(true)}
        onNewAgent={() => setShowAgentModal(true)}
        onDeleteSession={handleDeleteSession}
        onDeleteAgent={handleDeleteAgent}
      />

      <main className="flex-1 flex flex-col bg-white overflow-hidden">
        {activeId ? (
          <ChatWindow sessionId={activeId} />
        ) : (
          <div className="flex-1 flex items-center justify-center text-gray-400">
            <div className="text-center">
              <p className="text-lg font-medium">Nenhuma conversa selecionada</p>
              <p className="text-sm mt-1">Crie um agente e inicie uma conversa</p>
            </div>
          </div>
        )}
      </main>

      {showAgentModal && (
        <AgentModal onCreated={handleAgentCreated} onClose={() => setShowAgentModal(false)} />
      )}

      {showSessionModal && (
        <NewSessionModal
          agents={agents}
          onCreated={handleSessionCreated}
          onClose={() => setShowSessionModal(false)}
        />
      )}
    </div>
  );
}
