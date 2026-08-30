"use client";

import { Dialog, DialogContent, DialogTitle, IconButton } from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import type { Agent } from "@/types";
import { AgentForm } from "./AgentForm";

interface Props {
  agent?: Agent;
  onSaved: (agent: Agent) => void;
  onClose: () => void;
}

export function AgentModal({ agent, onSaved, onClose }: Props) {
  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        {agent ? "Editar agente" : "Novo agente"}
        <IconButton onClick={onClose} size="small">
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <AgentForm agent={agent} onSaved={onSaved} onCancel={onClose} />
      </DialogContent>
    </Dialog>
  );
}
