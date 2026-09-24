"use client";

import { useEffect, useState } from "react";
import { Autocomplete, Chip, TextField } from "@mui/material";
import SmartToyIcon from "@mui/icons-material/SmartToy";
import { agentApi } from "@/services/api";
import type { Agent } from "@/types";

interface Props {
  value: string[];
  onChange: (ids: string[]) => void;
  /** The agent being edited, if any — excluded from the options so it can't reference itself. */
  excludeAgentId?: string;
}

export function AgentMultiSelect({ value, onChange, excludeAgentId }: Props) {
  const [options, setOptions] = useState<Agent[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    agentApi
      .list()
      .then((res) => setOptions(res.agents.filter((a) => a.id !== excludeAgentId)))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [excludeAgentId]);

  const selected = options.filter((agent) => value.includes(agent.id));

  return (
    <Autocomplete
      multiple
      loading={loading}
      options={options}
      value={selected}
      getOptionLabel={(agent) => agent.name}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      onChange={(_, newValue) => onChange(newValue.map((agent) => agent.id))}
      noOptionsText="Nenhum outro agente disponível ainda."
      renderValue={(tagValue, getItemProps) =>
        tagValue.map((agent, index) => {
          const { key, ...itemProps } = getItemProps({ index });
          return (
            <Chip
              {...itemProps}
              key={key}
              label={agent.name}
              size="small"
              icon={<SmartToyIcon />}
              color="info"
              variant="outlined"
            />
          );
        })
      }
      renderInput={(params) => (
        <TextField
          {...params}
          placeholder={selected.length === 0 ? "Adicionar sub-agente..." : undefined}
          size="small"
        />
      )}
    />
  );
}
