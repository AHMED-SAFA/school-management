import { useState, useEffect } from 'react';
import {
  Container,
  AppBar,
  Toolbar,
  Typography,
  TextField,
  Box,
  Button,
  Paper,
  Divider,
  ThemeProvider,
  createTheme,
  CssBaseline,
} from '@mui/material';
import SchoolIcon from '@mui/icons-material/School';
import AddIcon from '@mui/icons-material/Add';
import ClassList from './components/ClassList';
import MyHolds from './components/MyHolds';
import CreateClassDialog from './components/CreateClassDialog';

const theme = createTheme({
  palette: {
    primary: {
      main: '#1565c0',
    },
    secondary: {
      main: '#388e3c',
    },
  },
});

const STUDENT_ID_KEY = 'school_admission_student_id';

function App() {
  const [studentId, setStudentId] = useState(() => {
    return localStorage.getItem(STUDENT_ID_KEY) || '';
  });
  const [studentIdInput, setStudentIdInput] = useState(() => {
    return localStorage.getItem(STUDENT_ID_KEY) || '';
  });
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [studentIdError, setStudentIdError] = useState('');

  useEffect(() => {
    if (studentId) {
      localStorage.setItem(STUDENT_ID_KEY, studentId);
    }
  }, [studentId]);

  const handleSetStudentId = () => {
    const trimmed = studentIdInput.trim();
    if (!trimmed) {
      setStudentIdError('Birth Registration Number is required.');
      return;
    }
    if (!/^\d{17}$/.test(trimmed)) {
      setStudentIdError('Birth Registration Number must be exactly 17 digits.');
      return;
    }
    setStudentIdError('');
    setStudentId(trimmed);
  };

  const handleClearStudentId = () => {
    setStudentId('');
    setStudentIdInput('');
    localStorage.removeItem(STUDENT_ID_KEY);
    setStudentIdError('');
  };

  const triggerRefresh = () => {
    setRefreshTrigger((prev) => prev + 1);
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <AppBar position="static" elevation={1}>
        <Toolbar>
          <SchoolIcon sx={{ mr: 1 }} />
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            School Admission System
          </Typography>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ py: 3 }}>
        {/* Student Identity Section */}
        <Paper sx={{ p: 2, mb: 3 }} variant="outlined">
          <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }} gutterBottom>
            Student Identity (Birth Registration Number)
          </Typography>
          <Box sx={{ display: 'flex', gap: 1, alignItems: 'flex-start' }}>
            <TextField
              size="small"
              placeholder="Enter 17-digit Birth Registration Number"
              value={studentIdInput}
              onChange={(e) => {
                setStudentIdInput(e.target.value);
                setStudentIdError('');
              }}
              onKeyDown={(e) => e.key === 'Enter' && handleSetStudentId()}
              error={!!studentIdError}
              helperText={studentIdError}
              sx={{ minWidth: 350 }}
              disabled={!!studentId}
              slotProps={{ htmlInput: { maxLength: 17 } }}
            />
            {!studentId ? (
              <Button variant="contained" onClick={handleSetStudentId} size="medium">
                Set
              </Button>
            ) : (
              <Button variant="outlined" color="error" onClick={handleClearStudentId} size="medium">
                Change
              </Button>
            )}
            {studentId && (
              <Typography variant="body2" color="success.main" sx={{ alignSelf: 'center', ml: 1 }}>
                ✓ Active: {studentId}
              </Typography>
            )}
          </Box>
        </Paper>

        {/* Class List Section */}
        <Paper sx={{ p: 2, mb: 3 }} variant="outlined">
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="h6">Available Classes</Typography>
            <Button
              variant="outlined"
              startIcon={<AddIcon />}
              onClick={() => setCreateDialogOpen(true)}
            >
              Create Class (Admin)
            </Button>
          </Box>
          <ClassList studentId={studentId} onHoldCreated={triggerRefresh} />
        </Paper>

        {/* My Applications Section */}
        <Paper sx={{ p: 2 }} variant="outlined">
          <Typography variant="h6" gutterBottom>
            My Applications
          </Typography>
          <Divider sx={{ mb: 2 }} />
          <MyHolds
            studentId={studentId}
            refreshTrigger={refreshTrigger}
            onActionComplete={triggerRefresh}
          />
        </Paper>
      </Container>

      {/* Create Class Dialog */}
      <CreateClassDialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        onCreated={triggerRefresh}
      />
    </ThemeProvider>
  );
}

export default App;
