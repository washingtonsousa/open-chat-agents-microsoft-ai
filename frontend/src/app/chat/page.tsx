"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Box, Typography } from "@mui/material";
import { agentApi, sessionApi } from "@/services/api";
import { AuthGuard } from "@/components/auth/AuthGuard";
import { AgentModal } from "@/components/agent/AgentModal";
import { ChatWindow } from "@/components/chat/ChatWindow";
import { SessionSidebar } from "@/components/chat/SessionSidebar";
import { NewSessionModal } from "@/components/session/NewSessionModal";
import type { Agent, Session } from "@/types";

function ChatContent() {
  const searchParams = useSearchParams();
  const [sessions, setSessions] = useState<Session[]>([]);
  const [agents, setAgents] = useState<Agent[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [showAgentModal, setShowAgentModal] = useState(false);
  const [editingAgent, setEditingAgent] = useState<Agent | undefined>(undefined);
  const [showSessionModal, setShowSessionModal] = useState(false);

  useEffect(() => {
    Promise.all([sessionApi.list(), agentApi.list()]).then(([s, a]) => {
      setSessions(s.sessions);
      setAgents(a.agents);
      const requested = searchParams.get("session");
      if (requested && s.sessions.some((session) => session.id === requested)) {
        setActiveId(requested);
      } else if (s.sessions.length > 0) {
        setActiveId(s.sessions[0].id);
      }
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function handleAgentSaved(agent: Agent) {
    setAgents((prev) => {
      const exists = prev.some((a) => a.id === agent.id);
      return exists ? prev.map((a) => (a.id === agent.id ? agent : a)) : [agent, ...prev];
    });
    setShowAgentModal(false);
    setEditingAgent(undefined);
  }

  function handleEditAgent(agent: Agent) {
    setEditingAgent(agent);
    setShowAgentModal(true);
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
    <Box sx={{ display: "flex", height: "100%" }}>
      <SessionSidebar
        sessions={sessions}
        agents={agents}
        activeSessionId={activeId}
        onSelect={setActiveId}
        onNewSession={() => setShowSessionModal(true)}
        onNewAgent={() => {
          setEditingAgent(undefined);
          setShowAgentModal(true);
        }}
        onEditAgent={handleEditAgent}
        onDeleteSession={handleDeleteSession}
        onDeleteAgent={handleDeleteAgent}
      />

      <Box component="main" sx={{ flex: 1, display: "flex", flexDirection: "column", overflow: "hidden" }}>
        {activeId ? (
          <ChatWindow sessionId={activeId} />
        ) : (
          <Box sx={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center" }}>
            <Box>
              <Typography variant="h6" color="text.secondary">
                Nenhuma conversa selecionada
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Crie um agente e inicie uma conversa
              </Typography>
            </Box>
          </Box>
        )}
      </Box>

      {showAgentModal && (
        <AgentModal
          agent={editingAgent}
          onSaved={handleAgentSaved}
          onClose={() => {
            setShowAgentModal(false);
            setEditingAgent(undefined);
          }}
        />
      )}

      {showSessionModal && (
        <NewSessionModal
          agents={agents}
          onCreated={handleSessionCreated}
          onClose={() => setShowSessionModal(false)}
        />
      )}
    </Box>
  );
}

export default function ChatPage() {
  return (
    <AuthGuard>
      <Suspense fallback={null}>
        <ChatContent />
      </Suspense>
    </AuthGuard>
  );
}
