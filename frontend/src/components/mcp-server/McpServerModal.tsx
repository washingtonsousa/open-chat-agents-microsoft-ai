"use client";

import { Dialog, DialogContent, DialogTitle, IconButton } from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import type { McpServer } from "@/types";
import { McpServerForm } from "./McpServerForm";

interface Props {
  onSaved: (server: McpServer) => void;
  onClose: () => void;
  editing?: McpServer;
}

export function McpServerModal({ onSaved, onClose, editing }: Props) {
  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        {editing ? "Editar servidor MCP" : "Novo servidor MCP"}
        <IconButton onClick={onClose} size="small">
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <McpServerForm onSaved={onSaved} onCancel={onClose} editing={editing} />
      </DialogContent>
    </Dialog>
  );
}
