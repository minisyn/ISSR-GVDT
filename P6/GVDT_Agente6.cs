using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GVDT_Agente6 : ISSR_Agent {
    enum GVDT_MsgCode
    {
        AvailableStone,
        NonAvailableStone,
        LetsGoToGoal
    }
    private Vector3 ps;
    public static int agentId = 0;

    public override void Start()
    {
        agentId++;
        Debug.LogFormat("{0}: comienza", Myself.Name);
    }

    public override IEnumerator Update()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            // En este punto generaremos un evento de tiempo
            current_event = ISSREventType.onTickElapsed;
            current_state = AgentStateMachine();
            Share();
        }
    }

    /*******************************************************
     *                  ESTADOS DEL AGENTE                 *
     *******************************************************/

    private ISSRState AgentStateMachine() // Función principal de máquina de estados
    {
        ISSRState next_state = current_state; // estado de salida, en principio igual
        switch (current_state) // Según el estado
        {
            case ISSRState.Idle:
                next_state = SF_Idle();
                break;
            case ISSRState.GoingToGripSmallStone:
                next_state = SF_GoingToGripSmallStone();
                break;
            case ISSRState.GoingToGoalWithSmallStone:
                next_state = SF_GoingToGoalWithSmallStone();
                break;
            case ISSRState.AvoidingObstacle:
                next_state = SF_AvoidingObstacle();
                break;
            case ISSRState.WaitforNoStonesMoving:
                next_state = SF_WaitforNoStonesMoving();
                break;
            case ISSRState.SleepingAfterCollisions:
                next_state = SF_SleepingAfterCollisions();
                break;
            case ISSRState.GoingToGripBigStone:
                next_state = SF_GoingToGripBigStone();
                break;
            case ISSRState.WaitingForHelpToMoveBigStone:
                next_state = SF_WaitingForHelpToMoveBigStone();
                break;
            case ISSRState.WaitforNoStonesMovingBigStone:
                next_state = SF_WaitforNoStonesMovingBigStone();
                break;
            case ISSRState.GoingToGoalWithBigStone:
                next_state = SF_GoingToGoalWithBigStone();
                break;
            case ISSRState.End:
                break;
            case ISSRState.Error:
                break;
            default:
                Debug.LogErrorFormat("{0}: estado {1} no considerado", Myself.Name, current_state);
                break;
        }
        if (current_state != next_state) // Si ha cambiado el estado
        {
            Debug.LogWarningFormat("{0}: Estado '{1}'-->'{2}' por evento '{3}'", Myself.Name, current_state, next_state, current_event);
        }
        return next_state;
    }

    private ISSRState SF_Idle()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onTickElapsed:
                // coge de la lista objeto más cercano a agente y se convierte en el objeto de interés:
                // focus_object = ISSRHelp.GetCloserToMeObjectInList(this, Valid_Small_Stones, ISSR_Type.SmallStone); 
                focus_object = ISSRHelp.Get_next_available_stone_closer_to_me(this);
                if (focus_object != null) // Si hay alguno (focus_object está definido)
                {
                    // Segun el tipo de piedra...
                    if (focus_object.type == ISSR_Type.SmallStone)
                    {
                        next_state = GetSStone(focus_object);
                    }
                    else if(focus_object.type == ISSR_Type.BigStone)
                    {
                        next_state = GetBStone(focus_object);
                    }
                    
                }
                else // focus_object es null, no hay más piedras disponibles
                {
                    Debug.LogFormat("{0}: no conozco más piedras pequeñas", Myself.Name);
                    next_state = ISSRState.End; // Fin del proceso
                }
                break;
            default:
                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    private ISSRState ProcessStone()
    {
        ISSRState next_state;
        if (oiSensable(focus_object))
        {
            Debug.LogFormat("{0}: '{1}' visible, trato de cogerla", Myself.Name,
            focus_object.Name);
            acGripObject(focus_object); // intento agarrar esa piedra pequeña cercana
        }
        else
        {
            Debug.LogFormat("{0}: '{1}' fuera de la vista, voy a su última posición",
            Myself.Name, focus_object.Name);
            acGotoLocation(oiLastLocation(focus_object));
        }
        if (acCheckError()) // si hay error en la acción:
        {
            next_state = ISSRState.Error; // Señala error
        }
        else
        { // En caso de que no haya error:
            next_state = ISSRState.GoingToGripSmallStone; // cambio a estado siguiente
        }

        return next_state;
    }

    private ISSRState SF_GoingToGripSmallStone()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onTickElapsed:
                // Comprobamos si la piedra que queremos coger sigue disponible
                next_state = GetSStone(focus_object);
                break;
            case ISSREventType.onGripSuccess: // Encuentra la piedra y la coge
                // Añadimos la marca de tiempos
                focus_object.TimeStamp = Time.time;
                // Eliminamos la piedra agarrada de la lista de piedras disponibles
                SStoneIsAvailable(focus_object, false);

                // Entra en espera.
                next_state = ISSRState.WaitforNoStonesMoving;
                break;

            case ISSREventType.onEnterSensingArea:
                if (object_just_seen.Equals(focus_object)) // veo justo la piedra que 'recuerdo'
                {
                    next_state = GetSStone(focus_object);
                }
                break;

            case ISSREventType.onCollision:
                if (colliding_object.Equals(focus_object))
                {
                    next_state = GetSStone(focus_object);
                }
                else
                {
                    next_state = ProcessCollision();
                }
                break;
            case ISSREventType.onDestArrived:
                // Llegamos al ultimo y la piedra no esta
                // Añadimos la marca de tiempos
                focus_object.TimeStamp = Time.time;
                // La descartamos
                SStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onGripFailure:
                // Añadimos la marca de tiempos
                focus_object.TimeStamp = Time.time;
                // Descartamos la piedra que se intenaba agarrar
                SStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onObjectLost:
                // Añadimos la marca de tiempos
                focus_object.TimeStamp = Time.time;
                // Descartamos la piedra que se intenaba agarrar
                SStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;
                break;
            default:
                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    private ISSRState SF_GoingToGoalWithSmallStone()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onGObjectScored:
                // Pasamos al estado Idle
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onCollision:
                next_state = ProcessCollision();
                break;
            case ISSREventType.onGObjectCollision:
                next_state = ProcessCollision();
                break;

            default:
                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    private ISSRState SF_AvoidingObstacle()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onCollision:
                next_state = ProcessCollision();
                break;
            case ISSREventType.onGObjectCollision:
                next_state = ProcessCollision();
                break;
            case ISSREventType.onGObjectScored:
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onDestArrived:
                next_state = ResumeAfterCollision();
                break;
            case ISSREventType.onManyCollisions:
                // Espera un tiempo aleatorio entre 0 y 2 segundos
                acSetTimer(UnityEngine.Random.value * 2f);
                next_state = ISSRState.SleepingAfterCollisions;
                break;
            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;

    }

    private ISSRState SF_WaitforNoStonesMoving()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onTickElapsed:
                // Sale de la espera si se cumple la condicion.
                if(iMovingStonesInMyTeam() == 0)
                {
                    acGotoLocation(iMyGoalLocation());
                    if (acCheckError()) // Si ha habido algún error en la acción
                    {
                        next_state = ISSRState.Error; // Señala error
                    }
                    else
                    { // En caso de que no haya error:
                        next_state = ISSRState.GoingToGoalWithSmallStone; // cambio a estado siguiente
                    }
                }
                break;
            case ISSREventType.onUngrip:
                // Añadimos la marca de tiempos
                focus_object.TimeStamp = Time.time;
                // Ponemos la piedra como disponible
                SStoneIsAvailable(focus_object, true);
                // pedimos obtenerla otra vez
                next_state = GetSStone(focus_object);
                break;
            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }

        return next_state;
    }

    private ISSRState SF_SleepingAfterCollisions()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onTimerOut:
                next_state = ResumeAfterCollision();
                break;
            case ISSREventType.onUngrip:
                focus_object.TimeStamp = Time.time;
                SStoneIsAvailable(focus_object, true);
                next_state = GetSStone(focus_object);
                break;
            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    private ISSRState SF_GoingToGripBigStone()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onTickElapsed:
                next_state = GetBStone(focus_object);
                break;
            case ISSREventType.onDestArrived:
            case ISSREventType.onObjectLost:
            case ISSREventType.onGripFailure:
                // Ponemos la marca de tiempos
                focus_object.TimeStamp = Time.time;

                // Descartamos la piedra
                BStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onCollision:
                // si el objecto con el que colisionas es el que tenia fijado
                if (colliding_object.Equals(focus_object))
                {
                    next_state = GetBStone(focus_object);
                }
                else
                {
                    next_state = ProcessCollision();
                }
                break;
            case ISSREventType.onEnterSensingArea:
                if (object_just_seen.Equals(focus_object)) // veo justo la piedra que 'recuerdo'
                {
                    next_state = GetBStone(focus_object);
                }
                break;
            case ISSREventType.onGripSuccess:
                // Comprobamos si hay algun agente agarrando la piedra
                if (oiGrippingAgents(focus_object) > 1)
                {
                    // Ponemos la marca de tiempos
                    focus_object.TimeStamp = Time.time;

                    // Descartamos la piedra
                    BStoneIsAvailable(focus_object, false);

                    // Cambiamos de estado
                    next_state = ISSRState.WaitforNoStonesMoving;
                }
                else
                {
                    // Esperamos a por ayuda
                    next_state = ISSRState.WaitingForHelpToMoveBigStone;
                }
                break;
            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    private ISSRState SF_WaitingForHelpToMoveBigStone()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onUngrip:
                next_state = GetBStone(focus_object);
                break;
            case ISSREventType.onAnotherAgentGripped:
                // Ponemos la marca de tiempos
                focus_object.TimeStamp = Time.time;

                // Descartamos la piedra
                BStoneIsAvailable(focus_object, false);

                // Cambiamos de estado
                next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;
            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    private ISSRState SF_WaitforNoStonesMovingBigStone()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onUngrip:
                // Ponemos la marca de tiempos
                focus_object.TimeStamp = Time.time;

                // Ponemos la piedra como disponible
                BStoneIsAvailable(focus_object, true);

                // intentamos coger la piedra
                next_state = GetBStone(focus_object);
                break;
            case ISSREventType.onAnotherAgentUngripped:
                // Ponemos la marca de tiempos
                focus_object.TimeStamp = Time.time;

                // Ponemos la piedra como disponible
                BStoneIsAvailable(focus_object, true);

                // Cambiamos de estado
                next_state = ISSRState.WaitingForHelpToMoveBigStone;
                break;
            case ISSREventType.onTickElapsed:
                // Comprobamos si hay alguna piedra en movimiento
                if(iMovingStonesInMyTeam() == 0){
                    // Enviamos un mensaje
                    acSendMsgObj( ISSRMsgCode.Assert, (int)GVDT_MsgCode.LetsGoToGoal, focus_object);
                    // Pedimos ir a la meta
                    acGotoLocation(iMyGoalLocation());
                    if (acCheckError()) // Si ha habido algún error en la acción
                    {
                        next_state = ISSRState.Error; // Señala error
                    }
                    else
                    { // En caso de que no haya error:
                        next_state = ISSRState.GoingToGoalWithBigStone; // cambio a estado siguiente
                    }
                }
                break;
            case ISSREventType.onMsgArrived:
                Debug.LogErrorFormat("{0} Es el codigo de mensaje", user_msg_code);
                if (user_msg_code == (int)GVDT_MsgCode.LetsGoToGoal && msg_obj.Equals(GrippedObject))
                {
                    // Pedimos ir a la meta
                    acGotoLocation(iMyGoalLocation());
                    if (acCheckError()) // Si ha habido algún error en la acción
                    {
                        next_state = ISSRState.Error; // Señala error
                    }
                    else
                    { // En caso de que no haya error:
                        next_state = ISSRState.GoingToGoalWithBigStone; // cambio a estado siguiente
                    }
                }
                break;
            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    private ISSRState SF_GoingToGoalWithBigStone()
    {
        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onPushTimeOut:
                next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;
            case ISSREventType.onGObjectScored:
                next_state = ISSRState.Idle;
                break;
            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }


    /*******************************************************
     *               FIN ESTADOS DEL AGENTE                *
     *******************************************************/

    /*******************************************************
     *                      OVERRIDES                      *
     *******************************************************/

    public override void onGripSuccess(ISSR_Object obj_gripped) //Objeto agarrado por agente
    { // Acabo de agarrar un objeto: acGripObject() completado con éxito

        Debug.LogFormat("{0}: agarra '{1}'", Myself.Name, obj_gripped.Name);
        current_state = AgentStateMachine(); // Llamar a máquina de estados
    }

    public override void onGObjectScored(ISSR_Object stone_that_scored)
    { // Acabo de puntuar/ meter una piedra en la meta
        Debug.LogFormat("{0}: piedra '{1}' metida en meta", Myself.Name, stone_that_scored.Name);
        current_state = AgentStateMachine(); // Llamar a máquina de estados
    }

    public override void onCollision(ISSR_Object obj_that_collided_with_me)
    {
        Debug.LogFormat("{0}: '{1}' colisiona conmigo.", Myself.Name, obj_that_collided_with_me.Name);
        colliding_object = obj_that_collided_with_me;
        current_state = AgentStateMachine();
    }

    public override void onGObjectCollision(ISSR_Object obj_that_collided_with_gripped_obj)
    {
        Debug.LogFormat("{0}: la piedra '{1}' que quiero agarrar colisiona conmigo.", Myself.Name, obj_that_collided_with_gripped_obj.Name);
        colliding_object = obj_that_collided_with_gripped_obj;
        current_state = AgentStateMachine();
    }

    public override void onDestArrived()
    {
        current_state = AgentStateMachine();
    }

    public override void onEnterSensingArea(ISSR_Object obj) // Comienzo a ver objeto obj
    { // Acaba de entrar un objeto en el ‘campo visual’ del agente.

        object_just_seen = obj; // Anotamos objeto que vemos

        if(obj.type == ISSR_Type.BigStone) // Si es una piedra grande
        {
            if (oiGrippingAgents(obj) > 1) // Su ibj esta agarrado por 2 agentes
            {
                BStoneIsAvailable(obj, false); // Se descarta: no disponible
            }
            else
            {
                BStoneIsAvailable(obj, true); // Se admite: disponible
            }
        }

        if (obj.type == ISSR_Type.SmallStone) // Si es una piedra pequeña
        {
            if (oiGrippingAgents(obj) > 0) // Si obj agarrado por agente
            {
                SStoneIsAvailable(obj, false); // Se descarta: no disponible
            }
            else
            {
                SStoneIsAvailable(obj, true); // Se admite: disponible
            }
        }

        current_state = AgentStateMachine();
    }

    public override void onGripFailure(ISSR_Object obj_I_wanted_to_grip)
    {
        current_state = AgentStateMachine();
    }

    public override void onObjectLost(ISSR_Object obj_i_was_looking_for)
    {
        current_state = AgentStateMachine();
    }

    public override void onUngrip(ISSR_Object ungripped_object)
    {
        current_state = AgentStateMachine();

    }
    public override void onMsgArrived(ISSR_Message msg)
    {
        ProcessMessage(msg);
    }

    public override void onManyCollisions()
    {
        current_state = AgentStateMachine();
    }

    public override void onTimerOut(float delay)
    {
        current_state = AgentStateMachine();
    }

    public override void onPushTimeOut(ISSR_Object gripped_big_stone)
    {
        current_state = AgentStateMachine();
    }

    public override void onAnotherAgentGripped(ISSR_Object agent)
    {
        current_state = AgentStateMachine();
    }

    public override void onAnotherAgentUngripped(ISSR_Object agent)
    {
        current_state = AgentStateMachine();
    }

    /*******************************************************
     *                  FIN OVERRIDES                      *
     *******************************************************/

    /*******************************************************
     *                FUNCIONES AUXILIARES                 *
     *******************************************************/

    ISSRState ProcessCollision()  // Procesar colision 
    {
        ISSRState next_state = current_state;

        switch (current_state)
        {
            case ISSRState.GoingToGripBigStone:
                // Guardamos el estado actual para recuperarlo mas adelante
                last_state = current_state;
                next_state = IrAPosicionSegura();
                break;
            case ISSRState.GoingToGripSmallStone:
                // Guardamos el estado actual para recuperarlo mas adelante
                last_state = current_state;
                next_state = IrAPosicionSegura();
                break;
            case ISSRState.GoingToGoalWithSmallStone:
                // Guardamos el estado actual para recuperarlo mas adelante
                last_state = current_state;
                next_state = IrAPosicionSegura();
                break;
            case ISSRState.AvoidingObstacle:
                next_state = IrAPosicionSegura();
                break;
            default:
                Debug.LogErrorFormat("ProcessCollision() en {0}, estado {1} no considerado al colisionar", Myself.Name, current_state);
                break;
        }

        return next_state;
    }

    private ISSRState IrAPosicionSegura()
    {
        // Calculamos la posición segura
        ps = ISSRHelp.CalculateSafeLocation(this, colliding_object);
        // Pide la acción de ir a la posición segura
        acGotoLocation(ps);
        if (acCheckError()) // Si ha habido algún error en la acción
        {
            return ISSRState.Error; // Señala error
        }
        else
        { // En caso de que no haya error:
           return ISSRState.AvoidingObstacle; // cambio a estado siguiente
        }
    }

    ISSRState ResumeAfterCollision()
    { // Continuar con lo que se estaba haciendo en el momento de la colisión.
        ISSRState next_state = current_state;

        switch (last_state)  // Según estado anterior 
        {
            case ISSRState.GoingToGripBigStone:
                next_state = GetBStone(focus_object);
                break;
            case ISSRState.GoingToGripSmallStone:
                next_state = GetSStone(focus_object);
                // next_state = ProcessStone();  // Volver a pedir coger piedra o ir a su lugar
                break;
            case ISSRState.GoingToGoalWithSmallStone:

                if(iMovingStonesInMyTeam() > 0)
                { // Si hay piedras en movimiento en mi equipo
                    next_state = ISSRState.WaitforNoStonesMoving;
                }
                else
                {
                    acGotoLocation(iMyGoalLocation());  // volver a pedir ir a la meta
                    if (acCheckError())
                    {
                        next_state = ISSRState.Error;
                    }
                    else
                    {
                        next_state = ISSRState.GoingToGoalWithSmallStone;
                    }  
                }   
                break;
            default:
                Debug.LogErrorFormat("{0}, estado {1} no considerado al volver de colisión", Myself.Name, last_state);
                break;
        }
        return next_state;
    }

    private void SStoneIsAvailable(ISSR_Object stone, bool available)
    {
        ISSRHelp.UpdateStoneLists(stone, available, Valid_Small_Stones, Invalid_Small_Stones);
    }

    private void BStoneIsAvailable(ISSR_Object stone, bool available)
    {
        ISSRHelp.UpdateStoneLists(stone, available, Valid_Big_Stones, Invalid_Big_Stones);
    }

    private ISSRState GetSStone(ISSR_Object small_stone)
    {
        ISSRState next_state = current_state;
        // Comprobamos si la piedra esta visible
        if (oiSensable(small_stone))
        { // Si esta visible
            // Comprobamos si la piedra esta agarrada por un agente
            if (oiGrippingAgents(small_stone) > 0)
            { // Si esta agarrada por un agente
                // Añadimos la marca de tiempos
                small_stone.TimeStamp = Time.time;
                // Descartamos la piedra y paramos
                SStoneIsAvailable(small_stone, false);
                acStop();
                if (acCheckError()) // Comprobamos si hay un error
                {
                    next_state = ISSRState.Error;
                }
                else
                {
                    next_state = ISSRState.Idle;
                }
            }
            else
            { // No esta agarrada por un agente
                // Pedimos agarrar la piedra
                acGripObject(small_stone);
                next_state = ISSRState.GoingToGripSmallStone;
            }
        }
        else
        { // No esta visible
            // Comprobamos si la piedra sigue o no
            if (ISSRHelp.Distance_from_object_to_me(this, focus_object) < iSensingRange())
            { // La piedra ya no esta
                // Añadimos la marca de tiempos
                small_stone.TimeStamp = Time.time;
                // Descartamos la piedra y paramos
                SStoneIsAvailable(small_stone, false);
                acStop();
                next_state = ISSRState.Idle;
            }
            else
            {
                // Va a la ultima posicion
                acGotoLocation(oiLastLocation(small_stone));
                if (acCheckError()) // si hay error en la acción:
                {
                    next_state = ISSRState.Error; // Señala error
                }
                else
                { // En caso de que no haya error:
                    next_state = ISSRState.GoingToGripSmallStone; // cambio a estado siguiente
                }
            }
        }

        return next_state;
    }

    private ISSRState GetBStone(ISSR_Object big_stone)
    {
        ISSRState next_state = current_state;

        // Comprobamos si la piedra esta visible
        if (oiSensable(big_stone))
        { // Si esta visible
            // Comprobamos si la piedra esta agarrada por dos agentes
            if (oiGrippingAgents(big_stone) > 1)
            { // Si esta agarrada por dos agentes
                // Añadimos la marca de tiempos
                big_stone.TimeStamp = Time.time;
                // Descartamos la piedra y paramos
                BStoneIsAvailable(big_stone, false);
                acStop();
                if (acCheckError()) // Comprobamos si hay un error
                {
                    next_state = ISSRState.Error;
                }
                else
                {
                    next_state = ISSRState.Idle;
                }
            }
            else
            { // Esta agarrada por 1 agente o por ninguno
                // Pedimos agarrar la piedra
                acGripObject(big_stone);
                next_state = ISSRState.GoingToGripBigStone;
            }
        }
        else
        { // No esta visible
            // Comprobamos si la piedra sigue o no
            if (ISSRHelp.Distance_from_object_to_me(this, focus_object) < iSensingRange())
            { // La piedra ya no esta
                // Añadimos la marca de tiempos
                big_stone.TimeStamp = Time.time;
                // Descartamos la piedra y paramos
                BStoneIsAvailable(big_stone, false);
                acStop();
                next_state = ISSRState.Idle;
            }
            else
            {
                // Va a la ultima posicion
                acGotoLocation(oiLastLocation(big_stone));
                if (acCheckError()) // si hay error en la acción:
                {
                    next_state = ISSRState.Error; // Señala error
                }
                else
                { // En caso de que no haya error:
                    next_state = ISSRState.GoingToGripBigStone; // cambio a estado siguiente
                }
            }
        }

        return next_state;
    }

    private void Share()
    {

        // Compartimos la informacion de las dos listas de piedras pequeñas

        foreach (ISSR_Object piedraDisponible in Valid_Small_Stones)
        {
            acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.AvailableStone, piedraDisponible);
        }

        foreach (ISSR_Object piedraNoDisponible in Invalid_Small_Stones)
        {
            acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.NonAvailableStone, piedraNoDisponible);
        }

        // Compartimos la informacion de las dos listas de piedras grandes
        foreach (ISSR_Object piedraDisponible in Valid_Big_Stones)
        {
            acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.AvailableStone, piedraDisponible);
        }

        foreach (ISSR_Object piedraNoDisponible in Invalid_Big_Stones)
        {
            acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.NonAvailableStone, piedraNoDisponible);
        }


        /*foreach (ISSR_Object piedra_invalida in Invalid_Small_Stones)
        {
            acSendMsgObj((int)GVDT_MsgCode.NonAvailableStone, agentId, piedra_invalida);
        }
        foreach (ISSR_Object piedra_valida in Valid_Small_Stones)
        {
            acSendMsgObj( (int) GVDT_MsgCode.AvailableStone, agentId, piedra_valida);
        }*/

    }

    private void ProcessMessage(ISSR_Message msg)
    {

        if (msg.usercode == (int)GVDT_MsgCode.LetsGoToGoal)
        {
            if (msg.Obj.Equals(focus_agent))
            {
                current_state = AgentStateMachine();
                return;
            }
        }
        else if (msg.usercode == (int)GVDT_MsgCode.AvailableStone)
        {
            if(msg.Obj.type == ISSR_Type.SmallStone)
            {
                SStoneIsAvailable(msg.Obj, true);
            }
            else
            {
                BStoneIsAvailable(msg.Obj, true);
            }
        }
        else if(msg.usercode == (int)GVDT_MsgCode.NonAvailableStone)
        {
            if (msg.Obj.type == ISSR_Type.SmallStone)
            {
                SStoneIsAvailable(msg.Obj, false);
            }
            else
            {
                BStoneIsAvailable(msg.Obj, false);
            }
        }

        /*switch (msg.code)
        {
            case (int)GVDT_MsgCode.AvailableStone:
                SStoneIsAvailable(msg.Obj, true);
                break;
            case (int)GVDT_MsgCode.NonAvailableStone:
                break;
        }*/
    }

    /*******************************************************
     *              FIN FUNCIONES AUXILIARES               *
     *******************************************************/
}
